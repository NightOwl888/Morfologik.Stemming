using J2N.IO;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Morfologik.Fsa
{
    /// <summary>
    /// An iterator that traverses the right language of a given node (all sequences
    /// reachable from a given node).
    /// </summary>
    public sealed class ByteSequenceEnumerator : IEnumerator<ByteBuffer>
    {
        /// <summary>
        /// Default expected depth of the recursion stack (estimated longest sequence
        /// in the automaton). Buffers expand by the same value if exceeded.
        /// </summary>
        private const int ExpectedMaxStates = 15;

        /// <summary>The FSA to which this iterator belongs.</summary>
        private readonly FSA fsa;

        /// <summary>An internal cache for the next element in the FSA</summary>
        private ByteBuffer? nextElement;

        /// <summary>
        /// A buffer for the current sequence of bytes from the current node to the
        /// root.
        /// </summary>
        private byte[] buffer = new byte[ExpectedMaxStates];

        /// <summary>Reusable byte buffer wrapper around <see cref="buffer"/>.</summary>
        private ByteBuffer bufferWrapper;

        /// <summary>An arc stack for DFS when processing the automaton.</summary>
        private int[] arcs = new int[ExpectedMaxStates];

        /// <summary>Current processing depth in <see cref="arcs"/>.</summary>
        private int position;

        private ByteBuffer? current;

        /// <summary>
        /// Gets a <see cref="ByteBuffer"/> with the sequence corresponding to the
        /// next final state in the automaton.
        /// </summary>
        public ByteBuffer Current => current!;

        /// <summary>
        /// Gets a <see cref="ByteBuffer"/> with the sequence corresponding to the
        /// next final state in the automaton.
        /// </summary>
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
            this.bufferWrapper = ByteBuffer.Wrap(buffer);
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
            bufferWrapper.Clear();
            nextElement = null;
            current = null;

            PushNode(node);
            return this;
        }

        /// <summary>
        /// Returns <c>true</c> if there are still elements in this enumerator.
        /// </summary>
        private bool HasNext()
        {
            if (nextElement == null)
            {
                nextElement = Advance();
            }

            return nextElement != null;
        }

        /// <summary>
        /// Returns a <see cref="ByteBuffer"/> with the sequence corresponding to the
        /// next final state in the automaton.
        /// </summary>
        /// <returns></returns>
        private ByteBuffer? Next()
        {
            if (nextElement != null)
            {
                ByteBuffer cache = nextElement;
                nextElement = null;
                return cache;
            }
            else
            {
                return Advance();
            }
        }

        /// <summary>
        /// Advances to the next available final state.
        /// </summary>
        private ByteBuffer? Advance()
        {
            if (position == 0)
            {
                return null;
            }

            while (position > 0)
            {
                int lastIndex = position - 1;
                int arc = arcs[lastIndex];

                if (arc == 0)
                {
                    // Remove the current node from the queue.
                    position--;
                    continue;
                }

                // Go to the next arc, but leave it on the stack
                // so that we keep the recursion depth level accurate.
                arcs[lastIndex] = fsa.GetNextArc(arc);

                // Expand buffer if needed.
                int bufferLength = this.buffer.Length;
                if (lastIndex >= bufferLength)
                {
                    Array.Resize(ref buffer, bufferLength + ExpectedMaxStates);
                    this.bufferWrapper = ByteBuffer.Wrap(buffer);
                }
                buffer[lastIndex] = fsa.GetArcLabel(arc);

                if (!fsa.IsArcTerminal(arc))
                {
                    // Recursively descend into the arc's node.
                    PushNode(fsa.GetEndNode(arc));
                }

                if (fsa.IsArcFinal(arc))
                {
                    bufferWrapper.Clear();
                    bufferWrapper.Limit = (lastIndex + 1);
                    return bufferWrapper;
                }
            }

            return null;
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
            if (!HasNext())
                return false;
            current = Next();
            return current != null;
        }

        /// <summary>
        /// Use <see cref="RestartFrom(int)"/> instead.
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Disposes resources associated with this instance.
        /// </summary>
        public void Dispose()
        {
            position = 0;
            bufferWrapper.Clear();
            nextElement = null;
            current = null!;
        }
    }
}
