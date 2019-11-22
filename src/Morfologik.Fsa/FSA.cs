using J2N.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Morfologik.Fsa
{
    /// <summary>
    /// This is a top abstract class for handling finite state automata. These
    /// automata are arc-based, a design described in Jan Daciuk's <i>Incremental
    /// Construction of Finite-State Automata and Transducers, and Their Use in the
    /// Natural Language Processing</i> (PhD thesis, Technical University of Gdansk).
    /// </summary>
    public abstract class FSA : IEnumerable<ByteBuffer>
    {
        /// <summary>
        /// Returns the identifier of the root node of this automaton. Returns
        /// 0 if the start node is also the end node (the automaton is empty).
        /// </summary>
        /// <returns>The identifier of the root node of this atomation. Returns
        /// 0 if the start node is also the end node (the automaton is empty).</returns>
        public abstract int GetRootNode();

        /// <summary>
        /// Returns the identifier of the first arc leaving <paramref name="node"/>
        /// or 0 if the node has no outgoing arcs.
        /// </summary>
        /// <param name="node">Identifier of the node.</param>
        /// <returns>Returns the identifier of the first arc leaving <paramref name="node"/>
        /// or 0 if the node has no outgoing arcs.</returns>
        public abstract int GetFirstArc(int node);

        /// <summary>
        /// Returns the identifier of the next arc after <paramref name="arc"/> and
        /// leaving <c>node</c>. Zero is returned if no more arcs are
        /// available for the node.
        /// </summary>
        /// <param name="arc">The arc's identifier.</param>
        /// <returns>Returns the identifier of the next arc after <paramref name="arc"/> and
        /// leaving <c>node</c>. Zero is returned if no more arcs are
        /// available for the node.</returns>
        public abstract int GetNextArc(int arc);

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
        public abstract int GetArc(int node, byte label);

        /// <summary>
        /// Return the label associated with a given <paramref name="arc"/>.
        /// </summary>
        /// <param name="arc">The arc's identifier.</param>
        /// <returns>Return the label associated with a given <paramref name="arc"/>.</returns>
        public abstract byte GetArcLabel(int arc);

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
        public abstract bool IsArcFinal(int arc);

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
        public abstract bool IsArcTerminal(int arc);

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
        public abstract int GetEndNode(int arc);

        /// <summary>
        /// Returns a set of flags for this FSA instance.
        /// </summary>
        public abstract FSAFlags Flags { get; }

        /// <summary>
        /// Calculates and returns the number of arcs of a given node.
        /// </summary>
        /// <param name="node">Identifier of the node.</param>
        /// <returns>Calculates and returns the number of arcs of a given node.</returns>
        public virtual int GetArcCount(int node)
        {
            int count = 0;
            for (int arc = GetFirstArc(node); arc != 0; arc = GetNextArc(arc))
            {
                count++;
            }
            return count;
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
        /// The value can then be computed by manual count of <see cref="GetSequences()"/>.
        /// </exception>
        public virtual int GetRightLanguageCount(int node)
        {
            throw new NotSupportedException("Automaton not compiled with " + FSAFlags.Numbers);
        }

        /// <summary>
        /// Returns an enumerable over all binary sequences starting at the given FSA
        /// state (node) and ending in final nodes. This corresponds to a set of
        /// suffixes of a given prefix from all sequences stored in the automaton.
        /// <para/>
        /// The element of the returned enumerable is a <see cref="ByteBuffer"/> whose contents changes on
        /// each call to <see cref="IEnumerator.MoveNext()"/>. To keep the contents between calls
        /// to <see cref="IEnumerator.MoveNext()"/>, one must copy the buffer to some other
        /// location.
        /// <para/>
        /// <b>Important.</b> It is guaranteed that the returned byte buffer is backed
        /// by a byte array and that the content of the byte buffer starts at the
        /// array's index 0.
        /// </summary>
        /// <param name="node">Identifier of the starting node from which to return subsequences.</param>
        /// <returns>An <see cref="IEnumerable{ByteBuffer}"/> over all sequences encoded starting at the given node.</returns>
        public virtual IEnumerable<ByteBuffer> GetSequences(int node)
        {
            if (node == 0)
                return new ByteBuffer[0];

            return new ByteSequenceEnumerable(this, node);
        }

        private class ByteSequenceEnumerable : IEnumerable<ByteBuffer>
        {
            private readonly FSA fsa;
            private readonly int node;
            public ByteSequenceEnumerable(FSA fsa, int node)
            {
                this.fsa = fsa;
                this.node = node;
            }

            public IEnumerator<ByteBuffer> GetEnumerator()
            {
                return new ByteSequenceEnumerator(this.fsa, node);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// An alias of calling <see cref="GetEnumerator()"/> directly (<see cref="FSA"/> is also
        /// <see cref="IEnumerable"/>.
        /// </summary>
        /// <returns>Returns all sequences encoded in the automaton.</returns>
        public IEnumerable<ByteBuffer> GetSequences()
        {
            return GetSequences(GetRootNode());
        }

        /// <summary>
        /// Returns an enumerator over all binary sequences starting from the initial FSA
        /// state (node) and ending in final nodes. The returned enumerator is a
        /// <see cref="ByteBuffer"/> whose contents changes on each call to
        /// <see cref="IEnumerator.MoveNext()"/>. The keep the contents between calls to
        /// <see cref="IEnumerator.MoveNext()"/>, one must copy the buffer to some other location.
        /// <para/>
        /// <b>Important.</b> It is guaranteed that the returned byte buffer is backed
        /// by a byte array and that the content of the byte buffer starts at the
        /// array's index 0.
        /// </summary>
        public IEnumerator<ByteBuffer> GetEnumerator()
        {
            return GetSequences().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Visit all states. The order of visiting is undefined. This method may be
        /// faster than traversing the automaton in post or preorder since it can scan
        /// states linearly. Returning false from <see cref="IStateVisitor.Accept(int)"/>
        /// immediately terminates the traversal.
        /// </summary>
        /// <typeparam name="T">n implementation of <see cref="IStateVisitor"/>.</typeparam>
        /// <param name="v">Visitor to receive traversal calls.</param>
        /// <returns>Returns the argument (for access to anonymous class fields).</returns>
        public virtual T VisitAllStates<T>(T v) where T : IStateVisitor
        {
            return VisitInPostOrder(v);
        }

        /// <summary>
        /// Same as <see cref="VisitInPostOrder{T}(T, int)"/>, starting from root
        /// automaton node.
        /// </summary>
        /// <typeparam name="T">An implementation of <see cref="IStateVisitor"/>.</typeparam>
        /// <param name="v">Visitor to receive traversal calls.</param>
        /// <returns>Returns the argument (for access to anonymous class fields).</returns>
        public virtual T VisitInPostOrder<T>(T v) where T : IStateVisitor
        {
            return VisitInPostOrder(v, GetRootNode());
        }

        /// <summary>
        /// Visits all states reachable from <paramref name="node"/> in postorder. Returning
        /// <c>false</c> from <see cref="IStateVisitor.Accept(int)"/> immediately terminates the
        /// traversal.
        /// </summary>
        /// <typeparam name="T">An implementation of <see cref="IStateVisitor"/>.</typeparam>
        /// <param name="v">Visitor to receive traversal calls.</param>
        /// <param name="node">Identifier of the node.</param>
        /// <returns>Returns the argument (for access to anonymous class fields).</returns>
        public virtual T VisitInPostOrder<T>(T v, int node) where T : IStateVisitor
        {
            VisitInPostOrder(v, node, new BitArray(1));
            return v;
        }

        /// <summary>Private recursion.</summary>
        private bool VisitInPostOrder(IStateVisitor v, int node, BitArray visited)
        {
            if (visited.Get(node))
                return true;
            visited.Set(node, true);

            for (int arc = GetFirstArc(node); arc != 0; arc = GetNextArc(arc))
            {
                if (!IsArcTerminal(arc))
                {
                    if (!VisitInPostOrder(v, GetEndNode(arc), visited))
                        return false;
                }
            }

            return v.Accept(node);
        }

        /// <summary>
        /// Same as <see cref="VisitInPreOrder{T}(T, int)"/>, starting from root
        /// automaton node.
        /// </summary>
        /// <typeparam name="T">An implementation of <see cref="IStateVisitor"/>.</typeparam>
        /// <param name="v">Visitor to receive traversal calls.</param>
        /// <returns>Returns the argument (for access to anonymous class fields).</returns>
        public virtual T VisitInPreOrder<T>(T v) where T : IStateVisitor
        {
            return VisitInPreOrder(v, GetRootNode());
        }

        /// <summary>
        /// Visits all states in preorder. Returning <c>false</c> from
        /// <see cref="IStateVisitor.Accept(int)"/> skips traversal of all sub-states of a
        /// given state.
        /// </summary>
        /// <typeparam name="T">An implementation of <see cref="IStateVisitor"/>.</typeparam>
        /// <param name="v">Visitor to receive traversal calls.</param>
        /// <param name="node">Identifier of the node.</param>
        /// <returns>Returns the argument (for access to anonymous class fields).</returns>
        public virtual T VisitInPreOrder<T>(T v, int node) where T : IStateVisitor
        {
            VisitInPreOrder(v, node, new BitArray(1));
            return v;
        }

        /// <summary>
        /// Reads all remaining bytes from an input stream and returns
        /// them as a byte array.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <returns>Reads all remaining bytes from an input stream and returns
        /// them as a byte array.</returns>
        /// <exception cref="IOException">Rethrown if an I/O exception occurs.</exception>
        internal static byte[] ReadRemaining(DataInputStream input)
        {
            using (var baos = new MemoryStream())
            {
                byte[] buffer = new byte[1024 * 8];
                int len;
                while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    baos.Write(buffer, 0, len);
                }
                return baos.ToArray();
            }
        }

        /// <summary>Private recursion.</summary>
        private void VisitInPreOrder(IStateVisitor v, int node, BitArray visited)
        {
            if (visited.Get(node))
            {
                return;
            }
            visited.Set(node, true);

            if (v.Accept(node))
            {
                for (int arc = GetFirstArc(node); arc != 0; arc = GetNextArc(arc))
                {
                    if (!IsArcTerminal(arc))
                    {
                        VisitInPreOrder(v, GetEndNode(arc), visited);
                    }
                }
            }
        }

        /// <summary>
        /// A factory for reading automata in any of the supported versions.
        /// </summary>
        /// <param name="stream">The input stream to read automaton data from. The stream is not disposed.</param>
        /// <returns>Returns an instantiated automaton. Never <c>null</c>.</returns>
        /// <exception cref="IOException">
        /// If the input stream does not represent an automaton, is otherwise
        /// invalid.
        /// </exception>
        public static FSA Read(Stream stream)
        {
            FSAHeader header = FSAHeader.Read(stream);

            switch (header.version)
            {
                case FSA5.Version:
                    return new FSA5(stream);
                case CFSA.Version:
                    return new CFSA(stream);
                case CFSA2.Version:
                    return new CFSA2(stream);
                default:
                    throw new IOException(
                        string.Format(CultureInfo.InvariantCulture, "Unsupported automaton version: 0x{0:2x}", header.version & 0xFF));
            }
        }

        /// <summary>
        /// A factory for reading a specific FSA subclass, including proper casting.
        /// </summary>
        /// <typeparam name="T">A subclass of <see cref="FSA"/> to cast the read automaton to.</typeparam>
        /// <param name="stream">The input stream to read automaton data from. The stream is not disposed.</param>
        /// <returns>Returns an instantiated automaton. Never <c>null</c>.</returns>
        /// <exception cref="IOException">
        /// If the input stream does not represent an automaton, is otherwise
        /// invalid or the class of the automaton read from the input stream
        /// is not assignable to <typeparamref name="T"/>.
        /// </exception>
        public static T Read<T>(Stream stream) where T : FSA
        {
            var fsa = Read(stream) as T;
            if (fsa == null)
                throw new IOException(string.Format(CultureInfo.InvariantCulture, "Expected FSA type {0}, but read an incompatible type {0}.",
                    typeof(T).Name, fsa.GetType().Name));
            return fsa;
        }
    }
}
