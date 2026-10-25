using System;

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
        /// Calculates the maximum number of bytes produced by encoding the specified number of
        /// source bytes and target bytes.
        /// </summary>
        /// <param name="sourceByteCount">The number of source bytes to encode.</param>
        /// <param name="targetByteCount">The number of target bytes to encode.</param>
        /// <returns>The maximum number of bytes produced by the encoding process.</returns>
        /// <seealso cref="TryEncode(ReadOnlySpan{byte}, ReadOnlySpan{byte}, Span{byte}, out int)"/>
        int GetMaxEncodedByteCount(int sourceByteCount, int targetByteCount);

        /// <summary>
        /// Calculates the maximum number of bytes produced by decoding the specified number of
        /// source bytes and encoded bytes.
        /// </summary>
        /// <param name="sourceByteCount">The number of source bytes to decode.</param>
        /// <param name="encodedByteCount">The number of encoded bytes to decode.</param>
        /// <returns>The maximum number of bytes produced by the decoding process.</returns>
        /// <seealso cref="TryDecode(ReadOnlySpan{byte}, ReadOnlySpan{byte}, Span{byte}, out int)"/>
        int GetMaxDecodedByteCount(int sourceByteCount, int encodedByteCount);

        /// <summary>
        /// Encodes into a span of bytes <paramref name="target"/> relative to <paramref name="source" />.
        /// </summary>
        /// <param name="source">The source byte sequence.</param>
        /// <param name="target">The target byte sequence to encode relative to <paramref name="source"/>.</param>
        /// <param name="destination">The span of bytes to write the encoded result to.</param>
        /// <param name="bytesWritten">Upon successful completion of the operation, the number of bytes encoded into <paramref name="destination"/>.</param>
        /// <returns><c>true</c> if the encoding was successful; otherwise, <c>false</c>.</returns>
        /// <seealso cref="GetMaxEncodedByteCount(int, int)"/>
        bool TryEncode(ReadOnlySpan<byte> source, ReadOnlySpan<byte> target, Span<byte> destination, out int bytesWritten);

        /// <summary>
        /// Decodes into a span of bytes <paramref name="encoded"/> relative to <paramref name="source" />.
        /// </summary>
        /// <param name="source">The source byte sequence.</param>
        /// <param name="encoded">The encoded byte sequence to decode.</param>
        /// <param name="destination">The span of bytes to write the decoded result to.</param>
        /// <param name="bytesWritten">Upon successful completion of the operation, the number of bytes encoded into <paramref name="destination"/>.</param>
        /// <returns><c>true</c> if the decoding was successful; otherwise, <c>false</c>.</returns>
        /// <seealso cref="GetMaxDecodedByteCount(int, int)"/>
        bool TryDecode(ReadOnlySpan<byte> source, ReadOnlySpan<byte> encoded, Span<byte> destination, out int bytesWritten);

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
