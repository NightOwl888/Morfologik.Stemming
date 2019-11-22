using J2N;
using J2N.IO;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Morfologik.Fsa
{
    /// <summary>
    /// CFSA (Compact Finite State Automaton) binary format implementation. This is a
    /// slightly reorganized version of <see cref="FSA5"/> offering smaller automata size
    /// at some (minor) performance penalty.
    ///
    /// <para><b>Note:</b> Serialize to <see cref="CFSA2"/> for new code.</para>
    ///
    /// <para>The encoding of automaton body is as follows.</para>
    /// 
    /// <code>
    /// ---- FSA header (standard)
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
    ///     4 | | | | | | | | | +------ version (fixed 0xc5)
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///     5 | | | | | | | | | +------ filler character
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///     6 | | | | | | | | | +------ annot character
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///     7 |C|C|C|C|G|G|G|G| +------ C - node data size (ctl), G - address size (gotoLength)
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///  8-32 | | | | | | | | | +------ labels mapped for type (1) of arc encoding. 
    ///       : : : : : : : : : |
    ///       +-+-+-+-+-+-+-+-+/
    /// 
    /// ---- Start of a node; only if automaton was compiled with NUMBERS option.
    /// 
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
    /// ---- A vector of node's arcs. Conditional format, depending on flags.
    /// 
    /// 1) NEXT bit set, mapped arc label. 
    /// 
    ///                +--------------- arc's label mapped in M bits if M's field value &gt; 0
    ///                | +------------- node pointed to is next
    ///                | | +----------- the last arc of the node
    ///         _______| | | +--------- the arc is final
    ///        /       | | | |
    ///       +-+-+-+-+-+-+-+-+\
    ///     0 |M|M|M|M|M|1|L|F| +------ flags + (M) index of the mapped label.
    ///       +-+-+-+-+-+-+-+-+/
    /// 
    /// 2) NEXT bit set, label separate.
    /// 
    ///                +--------------- arc's label stored separately (M's field is zero).
    ///                | +------------- node pointed to is next
    ///                | | +----------- the last arc of the node
    ///                | | | +--------- the arc is final
    ///                | | | |
    ///       +-+-+-+-+-+-+-+-+\
    ///     0 |0|0|0|0|0|1|L|F| +------ flags
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///     1 | | | | | | | | | +------ label
    ///       +-+-+-+-+-+-+-+-+/
    /// 
    /// 3) NEXT bit not set. Full arc.
    /// 
    ///                  +------------- node pointed to is next
    ///                  | +----------- the last arc of the node
    ///                  | | +--------- the arc is final
    ///                  | | |
    ///       +-+-+-+-+-+-+-+-+\
    ///     0 |A|A|A|A|A|0|L|F| +------ flags + (A) address field, lower bits
    ///       +-+-+-+-+-+-+-+-+/
    ///       +-+-+-+-+-+-+-+-+\
    ///     1 | | | | | | | | | +------ label
    ///       +-+-+-+-+-+-+-+-+/
    ///       : : : : : : : : :       
    ///       +-+-+-+-+-+-+-+-+\
    /// gtl-1 |A|A|A|A|A|A|A|A| +------ address, continuation (MSB)
    ///       +-+-+-+-+-+-+-+-+/
    /// </code>
    /// </summary>
    public sealed class CFSA : FSA
    {
        /// <summary>
        /// Automaton header version value.
        /// </summary>
        public const byte Version = (byte)0xC5;

        /// <summary>
        /// Bitmask indicating that an arc corresponds to the last character of a
        /// sequence available when building the automaton.
        /// </summary>
        public const int BitFinalArc = 1 << 0;

        /// <summary>
        /// Bitmask indicating that an arc is the last one of the node's list and the
        /// following one belongs to another node.
        /// </summary>
        public const int BitLastArc = 1 << 1;

        /// <summary>
        /// Bitmask indicating that the target node of this arc follows it in the
        /// compressed automaton structure (no goto field).
        /// </summary>
        public const int BitTargetNext = 1 << 2;

        /// <summary>
        /// An array of bytes with the internal representation of the automaton.
        /// Please see the documentation of this class for more information on how
        /// this structure is organized.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "design requires some writable array properties")]
        public byte[] Arcs { get; set; }

        /// <summary>
        /// The length of the node header structure (if the automaton was compiled with
        /// <see cref="FSAFlags.Numbers"/> option). Otherwise zero.
        /// </summary>
        public int NodeDataLength { get; private set; }

        /// <summary>
        /// Flags for this automaton version.
        /// </summary>
        private readonly FSAFlags flags;

        /// <summary>
        /// Number of bytes each address takes in full.
        /// </summary>
        public int GoToLength { get; private set; }

        /// <summary>
        /// Label mapping for arcs of type (1) (see class documentation). The array
        /// is indexed by mapped label's value and contains the original label.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "design requires some writable array properties")]
        public byte[] LabelMapping { get; private set; }

        /// <summary>
        /// Creates a new automaton, reading it from a file in FSA format, version 5.
        /// </summary>
        internal CFSA(Stream stream)
        {
            using (DataInputStream input = new DataInputStream(stream, true))
            {
                // Skip legacy header fields.
                input.ReadByte();  // filler
                input.ReadByte();  // annotation
                byte hgtl = (byte)input.ReadByte();

                /*
                 * Determine if the automaton was compiled with NUMBERS. If so, modify
                 * ctl and goto fields accordingly.
                 */
                flags = FSAFlags.Flexible | FSAFlags.StopBit | FSAFlags.NextBit;
                if ((hgtl & 0xf0) != 0)
                {
                    this.NodeDataLength = (hgtl.TripleShift(4)) & 0x0f;
                    this.GoToLength = hgtl & 0x0f;
                    flags |= FSAFlags.Numbers;
                }
                else
                {
                    this.NodeDataLength = 0;
                    this.GoToLength = hgtl & 0x0f;
                }

                /*
                 * Read mapping dictionary.
                 */
                LabelMapping = new byte[1 << 5];
                input.ReadFully(LabelMapping);

                /*
                 * Read arcs' data.
                 */
                Arcs = ReadRemaining(input);
            }
        }

        /// <summary>
        /// Returns the start node of this automaton. May return <c>0</c> if
        /// the start node is also an end node.
        /// </summary>
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
            if (0 == nodeOffset)
            {
                throw new Exception("This is a terminal arc [" + arc + "]");
            }
            return nodeOffset;
        }

        /// <summary>
        /// Return the label associated with a given <paramref name="arc"/>.
        /// </summary>
        /// <param name="arc">The arc's identifier.</param>
        /// <returns>Return the label associated with a given <paramref name="arc"/>.</returns>
        public override byte GetArcLabel(int arc)
        {
            if (IsNextSet(arc) && IsLabelCompressed(arc))
            {
                return this.LabelMapping[(Arcs[arc].TripleShift(3)) & 0x1f];
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
            Debug.Assert((Flags & FSAFlags.Numbers) != 0, $"This FSA was not compiled with {FSAFlags.Numbers}.");
            return FSA5.DecodeFromBytes(Arcs, node, NodeDataLength);
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
        /// Returns <c>true</c> if the label is compressed inside flags byte.
        /// </summary>
        /// <param name="arc">The node's arc identifier.</param>
        /// <returns>Returns <c>true</c> if the label is compressed inside flags byte.</returns>
        public bool IsLabelCompressed(int arc)
        {
            Debug.Assert(IsNextSet(arc), "Only applicable to arcs with NEXT bit.");
            return (Arcs[arc] & (-1 << 3)) != 0;
        }

        /// <summary>
        /// Returns a set of flags for this FSA instance.
        /// <para/>
        /// For this automaton version, an additional <see cref="FSAFlags.Numbers"/> flag
        /// may be set to indicate the automaton contains extra fields for each node.
        /// </summary>
        public override FSAFlags Flags => flags;

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
                int r = 0;
                for (int i = GoToLength; --i >= 1;)
                {
                    r = r << 8 | (Arcs[arc + 1 + i] & 0xff);
                }
                r = r << 8 | (Arcs[arc] & 0xff);
                return r.TripleShift(3);
            }
        }

        /// <summary>
        /// Read the arc's layout and skip as many bytes, as needed, to skip it.
        /// </summary>
        private int SkipArc(int offset)
        {
            if (IsNextSet(offset))
            {
                if (IsLabelCompressed(offset))
                {
                    offset++;
                }
                else
                {
                    offset += 1 + 1;
                }
            }
            else
            {
                offset += 1 + GoToLength;
            }
            return offset;
        }
    }
}
