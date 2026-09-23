using System;
using System.Diagnostics;

namespace Morfologik.Fsa
{
    /// <summary>
    /// This class implements some common matching and scanning operations on a
    /// generic FSA.
    /// </summary>
    public sealed class FSATraversal
    {
        /// <summary>
        /// Target automaton.
        /// </summary>
        private readonly FSA fsa;

        /// <summary>
        /// Traversals of the given FSA.
        /// </summary>
        /// <param name="fsa">The target automaton for traversals.</param>
        public FSATraversal(FSA fsa)
        {
            this.fsa = fsa;
        }

        /// <summary>
        /// Calculate perfect hash for a given input sequence of bytes. The perfect hash requires
        /// that <see cref="FSA"/> is built with <see cref="FSAFlags.Numbers"/> and corresponds to the sequential
        /// order of input sequences used at automaton construction time.
        /// </summary>
        /// <param name="sequence">The byte sequence to calculate perfect hash for.</param>
        /// <param name="node">The node to start traversal from, typically the root node (<see cref="FSA.GetRootNode()"/>).</param>
        /// <returns>
        /// Returns a unique integer assigned to the input sequence in the automaton (reflecting
        /// the number of that sequence in the input used to build the automaton). Returns a negative
        /// integer if the input sequence was not part of the input from which the automaton was created.
        /// The type of mismatch is a constant defined in <see cref="MatchResult"/>.
        /// </returns>
        /// <seealso cref="PerfectHash(ReadOnlySpan{byte})"/>
        public int PerfectHash(ReadOnlySpan<byte> sequence, int node)
        {
            int start = 0;
            int length = sequence.Length;

            Debug.Assert((fsa.Flags & FSAFlags.Numbers) != 0, $"FSA not built with {FSAFlags.Numbers} option.");

            if (sequence.IsEmpty)
                throw new ArgumentException("Must be a non-empty sequence.", nameof(sequence));

            int hash = 0;
            int end = start + length - 1;

            int seqIndex = start;
            byte label = sequence[seqIndex];

            // Seek through the current node's labels, looking for 'label', update hash.
            for (int arc = fsa.GetFirstArc(node); arc != 0;)
            {
                if (fsa.GetArcLabel(arc) == label)
                {
                    if (fsa.IsArcFinal(arc))
                    {
                        if (seqIndex == end)
                        {
                            return hash;
                        }

                        hash++;
                    }

                    if (fsa.IsArcTerminal(arc))
                    {
                        /* The automaton contains a prefix of the input sequence. */
                        return (int)MatchResultKind.AutomatonHasPrefix;
                    }

                    // The sequence is a prefix of one of the sequences stored in the automaton.
                    if (seqIndex == end)
                    {
                        return (int)MatchResultKind.SequenceIsAPrefix;
                    }

                    // Make a transition along the arc, go the target node's first arc.
                    arc = fsa.GetFirstArc(fsa.GetEndNode(arc));
                    label = sequence[++seqIndex];
                    continue;
                }
                else
                {
                    if (fsa.IsArcFinal(arc))
                    {
                        hash++;
                    }
                    if (!fsa.IsArcTerminal(arc))
                    {
                        hash += fsa.GetRightLanguageCount(fsa.GetEndNode(arc));
                    }
                }

                arc = fsa.GetNextArc(arc);
            }

            if (seqIndex > start)
            {
                return (int)MatchResultKind.AutomatonHasPrefix;
            }
            else
            {
                // Labels of this node ended without a match on the sequence. 
                // Perfect hash does not exist.
                return (int)MatchResultKind.NoMatch;
            }
        }

        /// <summary>
        /// Calculate perfect hash for a given input sequence of bytes. The perfect hash requires
        /// that <see cref="FSA"/> is built with <see cref="FSAFlags.Numbers"/> and corresponds to the sequential
        /// order of input sequences used at automaton construction time.
        /// </summary>
        /// <param name="sequence">The byte sequence to calculate perfect hash for.</param>
        /// <returns>
        /// Returns a unique integer assigned to the input sequence in the automaton (reflecting
        /// the number of that sequence in the input used to build the automaton). Returns a negative
        /// integer if the input sequence was not part of the input from which the automaton was created.
        /// The type of mismatch is a constant defined in <see cref="MatchResult"/>.
        /// </returns>
        /// <seealso cref="PerfectHash(ReadOnlySpan{byte}, int)"/>
        public int PerfectHash(ReadOnlySpan<byte> sequence)
        {
            return PerfectHash(sequence, fsa.GetRootNode());
        }

        /// <summary>
        /// Finds a matching path in the dictionary for a given sequence of labels from
        /// <paramref name="sequence"/> and starting at node <paramref name="node"/>.
        /// </summary>
        /// <param name="sequence">Input sequence to look for in the automaton.</param>
        /// <param name="node">The node to start traversal from, typically the root node (<see cref="FSA.GetRootNode()"/>).</param>
        /// <returns>A <see cref="MatchResult"/> with match <see cref="MatchResult.Kind"/>
        /// and other relevant fields.</returns>
        /// <seealso cref="Match(ReadOnlySpan{byte})"/>
        public MatchResult Match(ReadOnlySpan<byte> sequence, int node)
        {
            int start = 0;
            int length = sequence.Length;

            if (node == 0)
            {
                return new MatchResult(MatchResultKind.NoMatch, start, node);
            }

            FSA fsa = this.fsa;
            int end = start + length;
            for (int i = start; i < end; i++)
            {
                int arc = fsa.GetArc(node, sequence[i]);
                if (arc != 0)
                {
                    if (i + 1 == end && fsa.IsArcFinal(arc))
                    {
                        /* The automaton has an exact match of the input sequence. */
                        return new MatchResult(MatchResultKind.ExactMatch, i, node);
                    }

                    if (fsa.IsArcTerminal(arc))
                    {
                        /* The automaton contains a prefix of the input sequence. */
                        return new MatchResult(MatchResultKind.AutomatonHasPrefix, i + 1, node);
                    }

                    // Make a transition along the arc.
                    node = fsa.GetEndNode(arc);
                }
                else
                {
                    if (i > start)
                    {
                        return new MatchResult(MatchResultKind.AutomatonHasPrefix, i, node);
                    }
                    else
                    {
                        return new MatchResult(MatchResultKind.NoMatch, i, node);
                    }
                }
            }

            /* The sequence is a prefix of at least one sequence in the automaton. */
            return new MatchResult(MatchResultKind.SequenceIsAPrefix, 0, node);
        }

        /// <summary>
        /// Finds a matching path in the dictionary for a given sequence of labels from
        /// <paramref name="sequence"/>.
        /// </summary>
        /// <param name="sequence">Input sequence to look for in the automaton.</param>
        /// <returns><see cref="MatchResult"/> with updated match <see cref="MatchResult.Kind"/>.</returns>
        /// <seealso cref="Match(ReadOnlySpan{byte}, int)"/>
        public MatchResult Match(ReadOnlySpan<byte> sequence)
        {
            return Match(sequence, fsa.GetRootNode());
        }
    }
}
