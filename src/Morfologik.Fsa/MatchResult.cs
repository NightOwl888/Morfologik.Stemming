namespace Morfologik.Fsa
{
    /// <summary>
    /// A matching result returned from <see cref="FSATraversal"/>.
    /// </summary>
    /// <seealso cref="FSATraversal"/>
    public sealed class MatchResult
    {
        /// <summary>
        /// The automaton has exactly one match for the input sequence.
        /// </summary>
        public const int ExactMatch = 0;

        /// <summary>
        /// The automaton has no match for the input sequence and no sequence
        /// in the automaton is a prefix of the input.
        /// <para/>
        /// Note that to check for a general "input does not exist in the automaton"
        /// you have to check for both <see cref="NoMatch"/> and <see cref="AutomatonHasPrefix"/>.
        /// </summary>
        public const int NoMatch = -1;

        /// <summary>
        /// The automaton contains a prefix of the input sequence (but the
        /// full sequence does not exist). This translates to: one of the input sequences
        /// used to build the automaton is a prefix of the input sequence, but the
        /// input sequence contains a non-existent suffix.
        /// <para/>
        /// <see cref="Index"/> will contain an index of the
        /// first character of the input sequence not present in the
        /// dictionary.
        /// </summary>
        public const int AutomatonHasPrefix = -3;

        /// <summary>
        /// The sequence is a prefix of at least one sequence in the automaton.
        /// <see cref="Node"/> returns the node from which all sequences
        /// with the given prefix start in the automaton.
        /// </summary>
        public const int SequenceIsAPrefix = -4;

        /// <summary>
        /// One of the match types defined in this class.
        /// </summary>
        /// <seealso cref="NoMatch"/>
        /// <seealso cref="ExactMatch"/>
        /// <seealso cref="AutomatonHasPrefix"/>
        /// <seealso cref="SequenceIsAPrefix"/>
        public int Kind { get; set; }

        /// <summary>
        /// Input sequence's index, interpretation depends on <seealso cref="Kind"/>.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Automaton node, interpretation depends on the <seealso cref="Kind"/>.
        /// </summary>
        public int Node { get; set; }

        internal MatchResult(int kind, int index, int node) => Reset(kind, index, node);

        internal MatchResult(int kind) => Reset(kind, 0, 0);

        /// <summary>
        /// Intitializes a new instance of <see cref="MatchResult"/>.
        /// </summary>
        public MatchResult() => Reset(NoMatch, 0, 0);

        internal void Reset(int kind, int index, int node)
        {
            this.Kind = kind;
            this.Index = index;
            this.Node = node;
        }
    }
}
