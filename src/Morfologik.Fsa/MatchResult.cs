namespace Morfologik.Fsa
{
    /// <summary>
    /// A matching result returned from <see cref="FSATraversal"/>.
    /// </summary>
    /// <seealso cref="FSATraversal"/>
    public readonly struct MatchResult
    {
        /// <summary>
        /// One of the match types defined in this class.
        /// </summary>
        /// <seealso cref="MatchResultKind"/>
        public MatchResultKind Kind { get; }

        /// <summary>
        /// Input sequence's index, interpretation depends on <seealso cref="Kind"/>.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Automaton node, interpretation depends on the <seealso cref="Kind"/>.
        /// </summary>
        public int Node { get; }

        /// <summary>
        /// Intitializes a new instance of <see cref="MatchResult"/>.
        /// </summary>
        internal MatchResult(MatchResultKind kind, int index, int node)
        {
            Kind = kind;
            Index = index;
            Node = node;
        }
    }

    /// <summary>
    /// The type of match result.
    /// </summary>
    public enum MatchResultKind
    {
        /// <summary>
        /// The automaton has exactly one match for the input sequence.
        /// </summary>
        ExactMatch = 0,

        /// <summary>
        /// The automaton has no match for the input sequence and no sequence
        /// in the automaton is a prefix of the input.
        /// <para/>
        /// Note that to check for a general "input does not exist in the automaton"
        /// you have to check for both <see cref="NoMatch"/> and <see cref="AutomatonHasPrefix"/>.
        /// </summary>
        NoMatch = -1,

        /// <summary>
        /// The automaton contains a prefix of the input sequence (but the
        /// full sequence does not exist). This translates to: one of the input sequences
        /// used to build the automaton is a prefix of the input sequence, but the
        /// input sequence contains a non-existent suffix.
        /// <para/>
        /// <see cref="MatchResult.Index"/> will contain an index of the
        /// first character of the input sequence not present in the
        /// dictionary.
        /// </summary>
        AutomatonHasPrefix = -3,

        /// <summary>
        /// The sequence is a prefix of at least one sequence in the automaton.
        /// <see cref="MatchResult.Node"/> returns the node from which all sequences
        /// with the given prefix start in the automaton.
        /// </summary>
        SequenceIsAPrefix = -4
    }
}
