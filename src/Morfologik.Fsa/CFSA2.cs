using J2N;
using J2N.IO;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Morfologik.Fsa
{
    /// <summary>
    /// CFSA (Compact Finite State Automaton) binary format implementation, version 2:
    /// <list type="bullet">
    ///     <item><description><see cref="BitTargetNext"/> applicable on all arcs, not necessarily the last one.</description></item>
    ///     <item><description>v-coded goto field</description></item>
    ///     <item><description>v-coded perfect hashing numbers, if any</description></item>
    ///     <item><description>31 most frequent labels integrated with flags byte</description></item>
    /// </list>
    /// 
    /// <para>The encoding of automaton body is as follows.</para>
    /// 
    /// <code>
    /// ---- CFSA header
    /// Byte                            Description 
    ///       +-+-+-+-+-+-+-+-+\
    ///     0 | | | | | | | | | +------ '\'
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///     1 | | | | | | | | | +------ 'f'
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///     2 | | | | | | | | | +------ 's'
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///     3 | | | | | | | | | +------ 'a'
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///     4 | | | | | | | | | +------ version (fixed 0xc6)
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///     5 | | | | | | | | | +----\
    ///       +-+-+-+-+-+-+-+-+/      \ flags [MSB first]
    ///       +-+-+-+-+-+-+-+-+\      /
    ///     6 | | | | | | | | | +----/
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///     7 | | | | | | | | | +------ label lookup table size
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///  8-32 | | | | | | | | | +------ label value lookup table 
    ///       : : : : : : : : : |
    ///       +-+-+-+-+-+-+-+-+/
    /// 
    /// ---- Start of a node; only if automaton was compiled with NUMBERS option.
    /// 
    /// Byte
    ///        +-+-+-+-+-+-+-+-+\
    ///      0 | | | | | | | | | \  
    ///        +-+-+-+-+-+-+-+-+  +
    ///      1 | | | | | | | | |  |      number of strings recognized
    ///        +-+-+-+-+-+-+-+-+  +----- by the automaton starting
    ///        : : : : : : : : :  |      from this node. v-coding
    ///        +-+-+-+-+-+-+-+-+  +
    ///        | | | | | | | | | /  
    ///        +-+-+-+-+-+-+-+-+/
    ///
    /// ---- A vector of this node's arcs. An arc's layout depends on the combination of flags.
    /// 
    /// 1) NEXT bit set, mapped arc label. 
    /// 
    ///        +----------------------- node pointed to is next
    ///        | +--------------------- the last arc of the node
    ///        | | +------------------- this arc leads to a final state (acceptor)
    ///        | | |  _______+--------- arc's label; indexed if M &gt; 0, otherwise explicit label follows
    ///        | | | / | | | |
    ///       +-+-+-+-+-+-+-+-+\
    ///     0 |N|L|F|M|M|M|M|M| +------ flags + (M) index of the mapped label.
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///     1 | | | | | | | | | +------ optional label if M == 0
    ///       +-+-+-+-+-+-+-+-+/
    ///       : : : : : : : : :
    ///       +-+-+-+-+-+-+-+-+\
    ///       |A|A|A|A|A|A|A|A| +------ v-coded goto address
    ///       +-+-+-+-+-+-+-+-+/
    /// </code>
    /// </summary>
    public sealed class CFSA2 : FSA
    {
        /// <summary>
        /// Automaton header version value.
        /// </summary>
        public const byte Version = (byte)0xc6;

        /// <summary>
        /// The target node of this arc follows the last arc of the current state
        /// (no goto field).
        /// </summary>
        public const int BitTargetNext = 1 << 7;

        /// <summary>
        /// The arc is the last one from the current node's arcs list.
        /// </summary>
        public const int BitLastArc = 1 << 6;

        /// <summary>
        /// The arc corresponds to the last character of a sequence
        /// available when building the automaton (acceptor transition).
        /// </summary>
        public const int BitFinalArc = 1 << 5;

        /// <summary>
        /// The count of bits assigned to storing an indexed label.
        /// </summary>
        internal const int LabelIndexBits = 5;

        /// <summary>
        /// Masks only the M bits of a flag byte.
        /// </summary>
        internal const int LabelIndexMask = (1 << LabelIndexBits) - 1;

        /// <summary>
        /// Maximum size of the labels index.
        /// </summary>
        public const int LabelIndexSize = (1 << LabelIndexBits) - 1;

        /// <summary>
        /// An array of bytes with the internal representation of the automaton.
        /// Please see the <see cref="CFSA2"/> documentation for more information on how
        /// this structure is organized.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "design requires some writable array properties")]
        public byte[] Arcs { get; set; }

        /// <summary>
        /// Flags for this automaton version.
        /// </summary>
        private readonly FSAFlags flags;

        /// <summary>
        /// Label mapping for M-indexed labels.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "design requires some writable array properties")]
        public byte[] LabelMapping { get; private set; }

        /// <summary>
        /// If <c>true</c> states are prepended with numbers.
        /// </summary>
        private readonly bool hasNumbers;

        /// <summary>
        /// Epsilon node's offset.
        /// </summary>
        private readonly int epsilon = 0;

        /// <summary>
        /// Reads an automaton from a byte stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        internal CFSA2(Stream stream)
        {
            using (DataInputStream input = new DataInputStream(stream, true))
            {

                // Read flags.
                ushort flagBits = (ushort)input.ReadInt16();
                flags = 0;
                flags = (FSAFlags)flagBits;

                if (flagBits != (ushort)flags)
                {
                    throw new IOException($"Unrecognized flags: 0x{((int)flagBits).ToHexString()}");
                }

                this.hasNumbers = (flags & FSAFlags.Numbers) != 0;

                /*
                 * Read mapping dictionary.
                 */
                int labelMappingSize = input.ReadByte() & 0xff;

                LabelMapping = new byte[labelMappingSize];

                input.ReadFully(LabelMapping);

                /*
                 * Read arcs' data.
                 */
                Arcs = ReadRemaining(input);
            }
        }

        /// <summary>
        /// Returns the identifier of the root node of this automaton. Returns
        /// 0 if the start node is also the end node (the automaton is empty).
        /// </summary>
        /// <returns>The identifier of the root node of this atomation. Returns
        /// 0 if the start node is also the end node (the automaton is empty).</returns>
        public override int GetRootNode()
        {
            // Skip dummy node marking terminating state.
            return GetDestinationNodeOffset(GetFirstArc(epsilon));
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
            if (hasNumbers)
            {
                return SkipVInt32(node);
            }
            else
            {
                return node;
            }
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
            {
                return 0;
            }
            else
            {
                return SkipArc(arc);
            }
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
                {
                    return arc;
                }
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
            Debug.Assert(nodeOffset != 0, "Can't follow a terminal arc: " + arc);
            Debug.Assert(nodeOffset < Arcs.Length, "Node out of bounds.");
            return nodeOffset;
        }

        /// <summary>
        /// Return the label associated with a given <paramref name="arc"/>.
        /// </summary>
        /// <param name="arc">The arc's identifier.</param>
        /// <returns>Return the label associated with a given <paramref name="arc"/>.</returns>
        public override byte GetArcLabel(int arc)
        {
            int index = (sbyte)Arcs[arc] & LabelIndexMask;
            if (index > 0)
            {
                return this.LabelMapping[index];
            }
            else
            {
                return Arcs[arc + 1];
            }
        }

        /// <summary>
        /// Returns the number of sequences reachable from the given state if
        /// the automaton was compiled with <see cref="FSAFlags.Numbers"/>. The size
        /// of the right language of the state, in other words.
        /// </summary>
        /// <param name="node">Identifier of the node.</param>
        /// <returns>
        /// Returns the number of sequences reachable from the given state if
        /// the automaton was compiled with <see cref="FSAFlags.Numbers"/>. The size
        /// of the right language of the state, in other words.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// If the automaton was not compiled with <see cref="FSAFlags.Numbers"/>.
        /// The value can then be computed by manual count of <see cref="FSA.GetSequences()"/>.
        /// </exception>
        public override int GetRightLanguageCount(int node)
        {
            Debug.Assert((Flags & FSAFlags.Numbers) != 0, $"This FSA was not compiled with {FSAFlags.Numbers}");
            return ReadVInt32(Arcs, node);
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
            return (Arcs[arc] & BitFinalArc) != 0;
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
        /// Returns <c>true</c> if this arc has <c>NEXT</c> bit set.
        /// </summary>
        /// <param name="arc">The node's arc identifier.</param>
        /// <returns>Returns <c>true</c> if the argument is the last arc of a node.</returns>
        /// <seealso cref="BitLastArc"/>
        public bool IsArcLast(int arc)
        {
            return (Arcs[arc] & BitLastArc) != 0;
        }

        /// <summary>
        /// Returns <c>true</c> if <see cref="BitTargetNext"/> is set for this arc.
        /// </summary>
        /// <param name="arc">The node's arc identifier.</param>
        /// <returns>Returns <c>true</c> if <see cref="BitTargetNext"/> is set for this arc.</returns>
        /// <seealso cref="BitTargetNext"/>
        public bool IsNextSet(int arc)
        {
            return (Arcs[arc] & BitTargetNext) != 0;
        }

        /// <summary>
        /// Returns a set of flags for this FSA instance.
        /// </summary>
        public override FSAFlags Flags => flags;


        /// <summary>
        /// Returns the address of the node pointed to by this arc.
        /// </summary>
        internal int GetDestinationNodeOffset(int arc)
        {
            if (IsNextSet(arc))
            {
                /* Follow until the last arc of this state. */
                while (!IsArcLast(arc))
                {
                    arc = GetNextArc(arc);
                }

                /* And return the byte right after it. */
                return SkipArc(arc);
            }
            else
            {
                /*
                 * The destination node address is v-coded. v-code starts either
                 * at the next byte (label indexed) or after the next byte (label explicit).
                 */
                return ReadVInt32(Arcs, arc + (((sbyte)Arcs[arc] & LabelIndexMask) == 0 ? 2 : 1));
            }
        }

        /// <summary>
        /// Read the arc's layout and skip as many bytes, as needed, to skip it.
        /// </summary>
        private int SkipArc(int offset)
        {
            int flag = (sbyte)Arcs[offset++];

            // Explicit label?
            if ((flag & LabelIndexMask) == 0)
            {
                offset++;
            }

            // Explicit goto?
            if ((flag & BitTargetNext) == 0)
            {
                offset = SkipVInt32(offset);
            }

            Debug.Assert(offset < this.Arcs.Length);
            return offset;
        }

        /// <summary>
        /// Read a v-int.
        /// </summary>
        internal static int ReadVInt32(byte[] array, int offset)
        {
            sbyte b = (sbyte)array[offset];
            int value = b & 0x7F;

            for (int shift = 7; b < 0; shift += 7)
            {
                b = (sbyte)array[++offset];
                value |= (b & 0x7F) << shift;
            }

            return value;
        }

        /// <summary>
        /// Return the byte-length of a v-coded int.
        /// </summary>
        internal static int VInt32Length(int value)
        {
            Debug.Assert(value >= 0, "Can't v-code negative ints.");

            int bytes;
            for (bytes = 1; value >= 0x80; bytes++)
            {
                value >>= 7;
            }

            return bytes;
        }

        /// <summary>
        /// Skip a v-int.
        /// </summary>
        private int SkipVInt32(int offset)
        {
            while ((sbyte)Arcs[offset++] < 0)
            {
                // Do nothing.
            }
            return offset;
        }
    }
}
