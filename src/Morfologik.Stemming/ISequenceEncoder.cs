using J2N.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Morfologik.Stemming
{
    /// <summary>
    /// The logic of encoding one sequence of bytes relative to another sequence of
    /// bytes. The "base" form and the "derived" form are typically the stem of
    /// a word and the inflected form of a word.
    /// <para/>
    /// Derived form encoding helps in making the data for the automaton smaller
    /// and more repetitive (which results in higher compression rates).
    /// <para/>
    /// See example implementation for details.
    /// </summary>
    public interface ISequenceEncoder
    {
        /// <summary>
        /// Encodes <paramref name="target"/> relative to <paramref name="source"/>,
        /// optionally reusing the provided <see cref="ByteBuffer"/>.
        /// </summary>
        /// <param name="reuse">Reuses the provided <see cref="ByteBuffer"/> or allocates a new one if there is not enough remaining space.</param>
        /// <param name="source">The source byte sequence.</param>
        /// <param name="target">The target byte sequence to encode relative to <paramref name="source"/>.</param>
        /// <returns>Returns the <see cref="ByteBuffer"/> with encoded <paramref name="target"/>.</returns>
        ByteBuffer Encode(ByteBuffer reuse, ByteBuffer source, ByteBuffer target);

        /// <summary>
        /// Decodes <paramref name="encoded"/> relative to <paramref name="source"/>,
        /// optionally reusing the provided <see cref="ByteBuffer"/>.
        /// </summary>
        /// <param name="reuse">Reuses the provided <see cref="ByteBuffer"/> or allocates a new one if there is not enough remaining space.</param>
        /// <param name="source">The source byte sequence.</param>
        /// <param name="encoded">The previously encoded byte sequence.</param>
        /// <returns>Returns the <see cref="ByteBuffer"/> with decoded <paramref name="encoded"/>.</returns>
        ByteBuffer Decode(ByteBuffer reuse, ByteBuffer source, ByteBuffer encoded);

        /// <summary>
        /// The number of encoded form's prefix bytes that should be ignored (needed for separator lookup).
        /// An ugly workaround for GH-85, should be fixed by prior knowledge of whether the dictionary contains tags;
        /// then we can scan for separator right-to-left.
        /// <para/>
        /// See <a href="https://github.com/morfologik/morfologik-stemming/issues/85">https://github.com/morfologik/morfologik-stemming/issues/85</a>.
        /// </summary>
        [Obsolete]
        int PrefixBytes { get; }
    }
}
