using System;

namespace Morfologik.Fsa
{
    /// <summary>
    /// FSA automaton flags. Where applicable, flags follow Daciuk's <c>fsa</c>
    /// package.
    /// </summary>
    [Flags]
    public enum FSAFlags
    {
        /// <summary>
        /// Daciuk: flexible FSA encoding.
        /// </summary>
        Flexible = 1 << 0,

        /// <summary>
        /// Daciuk: stop bit in use.
        /// </summary>
        StopBit = 1 << 1,

        /// <summary>
        /// Daciuk: next bit in use.
        /// </summary>
        NextBit = 1 << 2,

        /// <summary>
        /// Daciuk: tails compression.
        /// </summary>
        Tails = 1 << 3,

        // These flags are outside of byte range (never occur in Daciuk's FSA).


        /// <summary>
        /// The FSA contains right-language count numbers on states.
        /// </summary>
        /// <seealso cref="FSA.GetRightLanguageCount(int)"/>
        Numbers = 1 << 8,

        /// <summary>
        /// The FSA supports legacy built-in separator and filler characters (Daciuk's
        /// FSA package compatibility).
        /// </summary>
        Separators = 1 << 9,
    }
}
