using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Represents the word data produced by a dictionary lookup.
    /// </summary>
    public abstract class WordData2
    {
        /// <summary>
        /// Gets the word that was looked up.
        /// </summary>
        public abstract ReadOnlyMemory<char> Word { get; }

        /// <summary>
        /// Gets the stem associated with the word.
        /// </summary>
        public abstract ReadOnlyMemory<char> Stem { get; }

        /// <summary>
        /// Gets the tag associated with the word.
        /// </summary>
        public abstract ReadOnlyMemory<char> Tag { get; }
    }
}
