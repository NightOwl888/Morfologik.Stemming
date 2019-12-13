using J2N.IO;
using J2N.Numerics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Morfologik.Fsa
{
    /// <summary>
    /// FSA binary format implementation for version 5.
    /// 
    /// <para/>
    /// Version 5 indicates the dictionary was built with these flags:
    /// <see cref="FSAFlags.Flexible"/>, <see cref="FSAFlags.StopBit"/> and
    /// <see cref="FSAFlags.NextBit"/>. The internal representation of the FSA must
    /// therefore follow this description (please note this format describes only a
    /// single transition (arc), not the entire dictionary file).
    /// 
    /// <code>
    /// ---- this node header present only if automaton was compiled with <see cref="FSAFlags.Numbers"/> option.
    /// Byte
    ///        +-+-+-+-+-+-+-+-+\
    ///      0 | | | | | | | | | \  LSB
    ///        +-+-+-+-+-+-+-+-+  +
    ///      1 | | | | | | | | |  |      number of strings recognized
    ///        +-+-+-+-+-+-+-+-+  +----- by the automaton starting
    ///        : : : : : : : : :  |      from this node.
    ///        +-+-+-+-+-+-+-+-+  +
    ///  ctl-1 | | | | | | | | | /  MSB
    ///        +-+-+-+-+-+-+-+-+/
    ///        
    /// ---- remaining part of the node
    /// 
    /// Byte
    ///       +-+-+-+-+-+-+-+-+\
    ///     0 | | | | | | | | | +------ label
    ///       +-+-+-+-+-+-+-+-+/
    /// 
    ///                  +------------- node pointed to is next
    ///                  | +----------- the last arc of the node
    ///                  | | +--------- the arc is final
    ///                  | | |
    ///             +-----------+
    ///             |    | | |  |
    ///         ___+___  | | |  |
    ///        /       \ | | |  |
    ///       MSB           LSB |
    ///        7 6 5 4 3 2 1 0  |
    ///       +-+-+-+-+-+-+-+-+ |
    ///     1 | | | | | | | | | \ \
    ///       +-+-+-+-+-+-+-+-+  \ \  LSB
    ///       +-+-+-+-+-+-+-+-+     +
    ///     2 | | | | | | | | |     |
    ///       +-+-+-+-+-+-+-+-+     |
    ///     3 | | | | | | | | |     +----- target node address (in bytes)
    ///       +-+-+-+-+-+-+-+-+     |      (not present except for the byte
    ///       : : : : : : : : :     |       with flags if the node pointed to
    ///       +-+-+-+-+-+-+-+-+     +       is next)
    ///   gtl | | | | | | | | |    /  MSB
    ///       +-+-+-+-+-+-+-+-+   /
    /// gtl+1                           (gtl = gotoLength)
    /// </code>
    /// </summary>
    public sealed class FSA5 : FSA
    {
        /// <summary>
        /// Default filler byte.
        /// </summary>
        public const byte DefaultFiller = (byte)'_';

        /// <summary>
        /// Default annotation byte.
        /// </summary>
        public const byte DefaultAnnotation = (byte)'+';

        /// <summary>
        /// Automaton version as in the file header.
        /// </summary>
        public const byte Version = 5;

        /// <summary>
        /// Bit indicating that an arc corresponds to the last character of a sequence
        /// available when building the automaton.
        /// </summary>
        public const int BitFinalArc = 1 << 0;

        /// <summary>
        /// Bit indicating that an arc is the last one of the node's list and the
        /// following one belongs to another node.
        /// </summary>
        public const int BitLastArc = 1 << 1;

        /// <summary>
        /// Bit indicating that the target node of this arc follows it in the
        /// compressed automaton structure (no goto field).
        /// </summary>
        public const int BitTargetNext = 1 << 2;

        /// <summary>
        /// An offset in the arc structure, where the address and flags field begins.
        /// In version 5 of FSA automata, this value is constant (1, skip label).
        /// </summary>
        public const int AddressOffset = 1;

        /// <summary>
        /// An array of bytes with the internal representation of the automaton. Please
        /// see the documentation of this class for more information on how this
        /// structure is organized.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "design requires some writable array properties")]
        public byte[] Arcs { get; private set; }

        /// <summary>
        /// The length of the node header structure (if the automaton was compiled with
        /// <see cref="FSAFlags.Numbers"/> option). Otherwise zero.
        /// </summary>
        public int NodeDataLength { get; private set; }

        /// <summary>
        /// Flags for this automaton version.
        /// </summary>
        private readonly FSAFlags flags;

        /// <summary>Number of bytes each address takes in full.</summary>
        public int GoToLength { get; private set; }

        /// <summary>Filler character.</summary>
        public byte Filler { get; private set; }

        /// <summary>Annotation character.</summary>
        public byte Annotation { get; private set; }

        /// <summary>
        /// Read and wrap a binary automaton in FSA version 5.
        /// </summary>
        internal FSA5(Stream stream)
        {
            DataInputStream input = new DataInputStream(stream);

            this.Filler = (byte)input.ReadByte();
            this.Annotation = (byte)input.ReadByte();
            byte hgtl = (byte)input.ReadByte();

            /*
             * Determine if the automaton was compiled with NUMBERS. If so, modify
             * ctl and goto fields accordingly.
             */
            flags = FSAFlags.Flexible | FSAFlags.StopBit | FSAFlags.NextBit;
            if ((hgtl & 0xf0) != 0)
            {
                flags |= FSAFlags.Numbers;
            }

            this.NodeDataLength = (hgtl.TripleShift(4)) & 0x0f;
            this.GoToLength = hgtl & 0x0f;

            Arcs = ReadRemaining(input);
        }

        /// <summary>
        /// Returns the start node of this automaton.
        /// </summary>
        /// <returns>Returns the start node of this automaton.</returns>
        public override int GetRootNode()
        {
            // Skip dummy node marking terminating state.
            int epsilonNode = SkipArc(GetFirstArc(0));

            // And follow the epsilon node's first (and only) arc.
            return GetDestinationNodeOffset(GetFirstArc(epsilonNode));
        }

        /// <summary>
        /// Returns the identifier of the first arc leaving <paramref name="node"/>
        /// or 0 if the node has no outgoing arcs.
        /// </summary>
        /// <param name="node">Identifier of the node.</param>
        /// <returns>Returns the identifier of the first arc leaving <paramref name="node"/>
        /// or 0 if the node has no outgoing arcs.</returns>
        public override sealed int GetFirstArc(int node)
        {
            return NodeDataLength + node;
        }

        /// <summary>
        /// Returns the identifier of the next arc after <paramref name="arc"/> and
        /// leaving <c>node</c>. Zero is returned if no more arcs are
        /// available for the node.
        /// </summary>
        /// <param name="arc">The arc's identifier.</param>
        /// <returns>Returns the identifier of the next arc after <paramref name="arc"/> and
        /// leaving <c>node</c>. Zero is returned if no more arcs are
        /// available for the node.</returns>
        public override sealed int GetNextArc(int arc)
        {
            if (IsArcLast(arc))
                return 0;
            else
                return SkipArc(arc);
        }

        /// <summary>
        /// Returns the identifier of an arc leaving <paramref name="node"/> and
        /// labeled with <paramref name="label"/>. An identifier equal to 0 means the
        /// node has no outgoing arc labeled <paramref name="label"/>.
        /// </summary>
        /// <param name="node">Identifier of the node.</param>
        /// <param name="label">The arc's label.</param>
        /// <returns>
        /// Returns the identifier of an arc leaving <paramref name="node"/> and
        /// labeled with <paramref name="label"/>. An identifier equal to 0 means the
        /// node has no outgoing arc labeled <paramref name="label"/>.
        /// </returns>
        public override int GetArc(int node, byte label)
        {
            for (int arc = GetFirstArc(node); arc != 0; arc = GetNextArc(arc))
            {
                if (GetArcLabel(arc) == label)
                    return arc;
            }

            // An arc labeled with "label" not found.
            return 0;
        }

        /// <summary>
        /// Return the end node pointed to by a given <paramref name="arc"/>.
        /// Terminal arcs (those that point to a terminal state) have no end
        /// node representation and throw a runtime exception.
        /// </summary>
        /// <param name="arc">The arc's identifier.</param>
        /// <returns>
        /// Return the end node pointed to by a given <paramref name="arc"/>.
        /// Terminal arcs (those that point to a terminal state) have no end
        /// node representation and throw a runtime exception.
        /// </returns>
        public override int GetEndNode(int arc)
        {
            int nodeOffset = GetDestinationNodeOffset(arc);
            Debug.Assert(nodeOffset != 0, "No target node for terminal arcs.");
            return nodeOffset;
        }

        /// <summary>
        /// Return the label associated with a given <paramref name="arc"/>.
        /// </summary>
        /// <param name="arc">The arc's identifier.</param>
        /// <returns>Return the label associated with a given <paramref name="arc"/>.</returns>
        public override byte GetArcLabel(int arc)
        {
            return Arcs[arc];
        }

        /// <summary>
        /// Returns <c>true</c> if the destination node at the end of
        /// this <paramref name="arc"/> corresponds to an input sequence created when
        /// building this automaton.
        /// </summary>
        /// <param name="arc">The arc's identifier.</param>
        /// <returns>
        /// Returns <c>true</c> if the destination node at the end of
        /// this <paramref name="arc"/> corresponds to an input sequence created when
        /// building this automaton.
        /// </returns>
        public override bool IsArcFinal(int arc)
        {
            return (Arcs[arc + AddressOffset] & BitFinalArc) != 0;
        }

        /// <summary>
        /// Returns <c>true</c> if this <paramref name="arc"/> does not have a
        /// terminating node (<see cref="GetEndNode(int)"/> will throw an
        /// exception). Implies <see cref="IsArcFinal(int)"/>.
        /// </summary>
        /// <param name="arc">The arc's identifier.</param>
        /// <returns>
        /// Returns <c>true</c> if this <paramref name="arc"/> does not have a
        /// terminating node (<see cref="GetEndNode(int)"/> will throw an
        /// exception). Implies <see cref="IsArcFinal(int)"/>.
        /// </returns>
        public override bool IsArcTerminal(int arc)
        {
            return (0 == GetDestinationNodeOffset(arc));
        }

        /// <summary>
        /// Returns the number encoded at the given node. The number equals the count
        /// of the set of suffixes reachable from <paramref name="node"/> (called its right
        /// language).
        /// </summary>
        public override int GetRightLanguageCount(int node)
        {
            Debug.Assert((Flags & FSAFlags.Numbers) != 0, $"This FSA was not compiled with {FSAFlags.Numbers}.");
            return DecodeFromBytes(Arcs, node, NodeDataLength);
        }

        /// <summary>
        /// Returns a set of flags for this FSA instance.
        /// <para/>
        /// For this automaton version, an additional <see cref="FSAFlags.Numbers"/> flag may
        /// be set to indicate the automaton contains extra fields for each node.
        /// </summary>
        public override FSAFlags Flags => flags;

        /// <summary>
        /// Returns <c>true</c> if this arc has <c>NEXT</c> bit set.
        /// </summary>
        /// <param name="arc">The node's arc identifier.</param>
        /// <returns>Returns <c>true</c> if the argument is the last arc of a node.</returns>
        public bool IsArcLast(int arc)
        {
            return (Arcs[arc + AddressOffset] & BitLastArc) != 0;
        }

        /// <summary>
        /// Returns <c>true</c> if <see cref="BitTargetNext"/> is set for this arc.
        /// </summary>
        /// <param name="arc">The node's arc identifier.</param>
        /// <returns>Returns <c>true</c> if <see cref="BitTargetNext"/> is set for this arc.</returns>
        /// <seealso cref="BitTargetNext"/>
        public bool IsNextSet(int arc)
        {
            return (Arcs[arc + AddressOffset] & BitTargetNext) != 0;
        }

        /// <summary>
        /// Returns an <paramref name="n"/>-byte integer encoded in byte-packed representation.
        /// </summary>
        internal static int DecodeFromBytes(byte[] arcs, int start, int n)
        {
            int r = 0;
            for (int i = n; --i >= 0;)
            {
                r = r << 8 | (arcs[start + i] & 0xff);
            }
            return r;
        }

        /// <summary>
        /// Returns the address of the node pointed to by this arc.
        /// </summary>
        internal int GetDestinationNodeOffset(int arc)
        {
            if (IsNextSet(arc))
            {
                /* The destination node follows this arc in the array. */
                return SkipArc(arc);
            }
            else
            {
                /*
                 * The destination node address has to be extracted from the arc's
                 * goto field.
                 */
                return DecodeFromBytes(Arcs, arc + AddressOffset, GoToLength).TripleShift(3);
            }
        }

        /// <summary>
        /// Read the arc's layout and skip as many bytes, as needed.
        /// </summary>
        private int SkipArc(int offset)
        {
            return offset +
                (IsNextSet(offset)
                    ? 1 + 1   /* label + flags */
                    : 1 + GoToLength /* label + flags/address */);
        }
    }
}
