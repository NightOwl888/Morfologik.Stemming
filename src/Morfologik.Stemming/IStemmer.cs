using J2N.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace Morfologik.Stemming
{
    /// <summary>
    /// A generic &quot;stemmer&quot; interface in Morfologik.
    /// </summary>
    public interface IStemmer
    {
        /// <summary>
        /// Returns a list of <see cref="WordData"/> entries for a given word. The returned
        /// list is never <code>null</code>. Depending on the stemmer's
        /// implementation the <see cref="WordData"/> may carry the stem and additional
        /// information (tag) or just the stem.
        /// <para/>
        /// The returned list and any object it contains are not usable after a
        /// subsequent call to this method. Any data that should be stored in between
        /// must be copied by the caller.
        /// </summary>
        /// <param name="word">The word (typically inflected) to look up base forms for.</param>
        /// <returns>A list of <see cref="WordData"/> entries (possibly empty).</returns>
        IList<WordData> Lookup(ICharSequence word);

        /// <summary>
        /// Returns a list of <see cref="WordData"/> entries for a given word. The returned
        /// list is never <code>null</code>. Depending on the stemmer's
        /// implementation the <see cref="WordData"/> may carry the stem and additional
        /// information (tag) or just the stem.
        /// <para/>
        /// The returned list and any object it contains are not usable after a
        /// subsequent call to this method. Any data that should be stored in between
        /// must be copied by the caller.
        /// </summary>
        /// <param name="word">The word (typically inflected) to look up base forms for.</param>
        /// <returns>A list of <see cref="WordData"/> entries (possibly empty).</returns>
        IList<WordData> Lookup(char[] word);

        /// <summary>
        /// Returns a list of <see cref="WordData"/> entries for a given word. The returned
        /// list is never <code>null</code>. Depending on the stemmer's
        /// implementation the <see cref="WordData"/> may carry the stem and additional
        /// information (tag) or just the stem.
        /// <para/>
        /// The returned list and any object it contains are not usable after a
        /// subsequent call to this method. Any data that should be stored in between
        /// must be copied by the caller.
        /// </summary>
        /// <param name="word">The word (typically inflected) to look up base forms for.</param>
        /// <returns>A list of <see cref="WordData"/> entries (possibly empty).</returns>
        IList<WordData> Lookup(StringBuilder word);

        /// <summary>
        /// Returns a list of <see cref="WordData"/> entries for a given word. The returned
        /// list is never <code>null</code>. Depending on the stemmer's
        /// implementation the <see cref="WordData"/> may carry the stem and additional
        /// information (tag) or just the stem.
        /// <para/>
        /// The returned list and any object it contains are not usable after a
        /// subsequent call to this method. Any data that should be stored in between
        /// must be copied by the caller.
        /// </summary>
        /// <param name="word">The word (typically inflected) to look up base forms for.</param>
        /// <returns>A list of <see cref="WordData"/> entries (possibly empty).</returns>
        IList<WordData> Lookup(string word);
    }
}
