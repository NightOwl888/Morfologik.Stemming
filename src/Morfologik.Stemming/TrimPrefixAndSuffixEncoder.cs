using System;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Encodes <c>dst</c> relative to <c>src</c> by trimming whatever
    /// non-equal suffix and prefix <c>src</c> and <c>dst</c> have. The
    /// output code is (bytes):
    /// 
    /// <code>
    /// {P}{K}{suffix}
    /// </code>
    /// 
    /// where (<c>P</c> - 'A') bytes should be trimmed from the start of
    /// <c>src</c>, (<c>K</c> - 'A') bytes should be trimmed from the
    /// end of <c>src</c> and then the <c>suffix</c> should be appended
    /// to the resulting byte sequence.
    /// 
    /// <para>
    /// Examples:
    /// </para>
    /// 
    /// <code>
    /// src: abc
    /// dst: abcd
    /// encoded: AAd
    /// 
    /// src: abc
    /// dst: xyz
    /// encoded: ADxyz
    /// </code>
    /// </summary>
    public sealed class TrimPrefixAndSuffixEncoder : ISequenceEncoder // Morfologik.Stemming specific - marked sealed to prevent inheritance and ensure singleton usage
    {
        private TrimPrefixAndSuffixEncoder() { } // Singleton only

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static TrimPrefixAndSuffixEncoder Instance { get; } = new TrimPrefixAndSuffixEncoder();

        /// <summary>
        /// Maximum encodable single-byte code.
        /// </summary>
        private const int RemoveEverything = 255;

        /// <inheritdoc cref="ISequenceEncoder.PrefixBytes"/>
        public int PrefixBytes => 2;

        /// <inheritdoc/>
        public int GetMaxEncodedByteCount(int sourceByteCount, int targetByteCount)
        {
            return checked(PrefixBytes + targetByteCount);
        }

        /// <inheritdoc/>
        public int GetMaxDecodedByteCount(int sourceByteCount, int encodedByteCount)
        {
            return checked(sourceByteCount + encodedByteCount - PrefixBytes);
        }

        /// <inheritdoc/>
        public bool TryEncode(ReadOnlySpan<byte> source, ReadOnlySpan<byte> target, Span<byte> destination, out int bytesWritten)
        {
            // Search for the maximum matching subsequence that can be encoded.
            int maxSubsequenceLength = 0;
            int maxSubsequenceIndex = 0;

            for (int i = 0; i < source.Length; i++)
            {
                int sharedPrefixLength = BufferUtils.SharedPrefixLength(
                    source.Slice(i),
                    target);

                // Only update maxSubsequenceLength if we will be able to
                // encode the amount of source that remains before and after
                // the matching subsequence.
                if (sharedPrefixLength > maxSubsequenceLength
                    && i < RemoveEverything
                    && source.Length - (i + sharedPrefixLength) < RemoveEverything)
                {
                    maxSubsequenceLength = sharedPrefixLength;
                    maxSubsequenceIndex = i;
                }
            }

            // Determine how much to remove (and where) from src to get a
            // prefix of dst.
            int truncatePrefixBytes = maxSubsequenceIndex;
            int truncateSuffixBytes =
                source.Length - (maxSubsequenceIndex + maxSubsequenceLength);

            if (truncatePrefixBytes >= RemoveEverything
                || truncateSuffixBytes >= RemoveEverything)
            {
                maxSubsequenceIndex = 0;
                maxSubsequenceLength = 0;
                truncatePrefixBytes = RemoveEverything;
                truncateSuffixBytes = RemoveEverything;
            }

            int suffixLength = target.Length - maxSubsequenceLength;
            int requiredLength = PrefixBytes + suffixLength;

            if (destination.Length < requiredLength)
            {
                bytesWritten = 0;
                return false;
            }

            destination[0] = (byte)((truncatePrefixBytes + 'A') & 0xFF);
            destination[1] = (byte)((truncateSuffixBytes + 'A') & 0xFF);

            target.Slice(maxSubsequenceLength).CopyTo(
                destination.Slice(PrefixBytes));

            bytesWritten = requiredLength;
            return true;
        }

        /// <inheritdoc/>
        public bool TryDecode(ReadOnlySpan<byte> source, ReadOnlySpan<byte> encoded, Span<byte> destination, out int bytesWritten)
        {
            if (encoded.Length < PrefixBytes)
            {
                throw new ArgumentException("Encoded sequence must be at least 2 bytes long.", nameof(encoded));
            }

            int truncatePrefixBytes = (encoded[0] - 'A') & 0xFF;
            int truncateSuffixBytes = (encoded[1] - 'A') & 0xFF;

            if (truncatePrefixBytes == RemoveEverything
                || truncateSuffixBytes == RemoveEverything)
            {
                truncatePrefixBytes = source.Length;
                truncateSuffixBytes = 0;
            }
            else
            {
                if (truncatePrefixBytes > source.Length)
                {
                    throw new ArgumentException("Encoded sequence requests removal of more prefix bytes than the source contains.", nameof(encoded));
                }

                if (truncateSuffixBytes > source.Length - truncatePrefixBytes)
                {
                    throw new ArgumentException("Encoded sequence requests removal of more suffix bytes than remain after removing the prefix.", nameof(encoded));
                }
            }

            int sourceLength = source.Length - truncatePrefixBytes - truncateSuffixBytes;
            int suffixLength = encoded.Length - PrefixBytes;
            int requiredLength = checked(sourceLength + suffixLength);

            if (destination.Length < requiredLength)
            {
                bytesWritten = 0;
                return false;
            }

            source.Slice(truncatePrefixBytes, sourceLength).CopyTo(destination);
            encoded.Slice(PrefixBytes).CopyTo(destination.Slice(sourceLength));

            bytesWritten = requiredLength;
            return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return nameof(TrimPrefixAndSuffixEncoder);
        }
    }
}
