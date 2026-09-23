using J2N.IO;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Morfologik.Fsa
{
    /// <summary>
    /// An enumerator that traverses the right language of a given node (all sequences
    /// reachable from a given node).
    /// </summary>
    /// <remarks>
    /// The returned memory is backed by a buffer reused by the enumerator. Its contents
    /// may change after a subsequent call to <see cref="IEnumerator.MoveNext"/>.
    /// Copy the contents if the sequence needs to be retained.
    /// </remarks>
    public sealed class ByteSequenceEnumerator : IEnumerator<ReadOnlyMemory<byte>>
    {
        /// <summary>
        /// Default expected depth of the recursion stack (estimated longest sequence
        /// in the automaton). Buffers expand by the same value if exceeded.
        /// </summary>
        private const int ExpectedMaxStates = 15;

        /// <summary>The FSA to which this iterator belongs.</summary>
        private readonly FSA fsa;

        /// <summary>
        /// A buffer for the current sequence of bytes from the current node to the
        /// root.
        /// </summary>
        private byte[] buffer = new byte[ExpectedMaxStates];

        /// <summary>An arc stack for DFS when processing the automaton.</summary>
        private int[] arcs = new int[ExpectedMaxStates];

        /// <summary>Current processing depth in <see cref="arcs"/>.</summary>
        private int position;

        private ReadOnlyMemory<byte> current;

        /// <summary>
        /// Gets the current byte sequence.
        /// </summary>
        /// <remarks>
        /// The returned memory is backed by storage reused by this enumerator and may be
        /// overwritten by the next call to <see cref="MoveNext"/>.
        /// </remarks>
        public ReadOnlyMemory<byte> Current => current!;

        object? IEnumerator.Current => current;

        /// <summary>
        /// Create an instance of the enumerator iterating over all automaton sequences.
        /// </summary>
        /// <param name="fsa">The automaton to iterate over.</param>
        public ByteSequenceEnumerator(FSA fsa)
            : this(fsa, fsa.GetRootNode())
        { }

        /// <summary>
        /// Create an instance of the enumerator for a given node.
        /// </summary>
        /// <param name="fsa">The automaton to iterate over.</param>
        /// <param name="node">The starting node's identifier (can be the <see cref="FSA.GetRootNode()"/>.</param>
        public ByteSequenceEnumerator(FSA fsa, int node)
        {
            this.fsa = fsa;

            if (fsa.GetFirstArc(node) != 0)
            {
                RestartFrom(node);
            }
        }

        /// <summary>
        /// Restart walking from <paramref name="node"/>. Allows iterator reuse.
        /// </summary>
        /// <param name="node">Restart the enumerator from <paramref name="node"/>.</param>
        /// <returns>Returns <c>this</c> for call chaining.</returns>
        public ByteSequenceEnumerator RestartFrom(int node)
        {
            position = 0;
            current = default;

            PushNode(node);
            return this;
        }

        // .NET doesn't support Remove()

        /// <summary>
        /// Descends to a given node, adds its arcs to the stack to be traversed.
        /// </summary>
        private void PushNode(int node)
        {
            // Expand buffers if needed.
            if (position == arcs.Length)
            {
                Array.Resize(ref arcs, arcs.Length + ExpectedMaxStates);
            }

            arcs[position++] = fsa.GetFirstArc(node);
        }

        /// <summary>
        /// Advances to the next available final state, if one is available.
        /// </summary>
        public bool MoveNext()
        {
            if (position == 0)
                return false;

            while (position > 0)
            {
                int lastIndex = position - 1;
                int arc = arcs[lastIndex];

                if (arc == 0)
                {
                    position--;
                    continue;
                }

                arcs[lastIndex] = fsa.GetNextArc(arc);

                int bufferLength = buffer.Length;
                if (lastIndex >= bufferLength)
                {
                    Array.Resize(ref buffer, bufferLength + ExpectedMaxStates);
                }

                buffer[lastIndex] = fsa.GetArcLabel(arc);

                if (!fsa.IsArcTerminal(arc))
                    PushNode(fsa.GetEndNode(arc));

                if (fsa.IsArcFinal(arc))
                {
                    current = buffer.AsMemory(0, lastIndex + 1);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Use <see cref="RestartFrom(int)"/> instead.
        /// </summary>
        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Disposes resources associated with this instance.
        /// </summary>
        public void Dispose()
        {
            position = 0;
            current = default;
        }
    }
}
