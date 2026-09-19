using System;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Encodes <c>dst</c> relative to <c>src</c> by trimming whatever
    /// non-equal suffix and infix <c>src</c> and <c>dst</c> have. The
    /// output code is (bytes):
    /// 
    /// <code>
    /// {X}{L}{K}{suffix}
    /// </code>
    /// 
    /// where <c>src's</c> infix at position (<c>X</c> - 'A') and of
    /// length (<c>L</c> - 'A') should be removed, then (<c>K</c> -
    /// 'A') bytes should be trimmed from the end and then the <c>suffix</c>
    /// should be appended to the resulting byte sequence.
    /// 
    /// <para>
    /// Examples:
    /// </para>
    /// 
    /// <code>
    /// src: ayz
    /// dst: abc
    /// encoded: AACbc
    /// 
    /// src: aillent
    /// dst: aller
    /// encoded: BBCr
    /// </code>
    /// </summary>
    public sealed class TrimInfixAndSuffixEncoder : ISequenceEncoder // Morfologik.Stemming specific - marked sealed to prevent inheritance and ensure singleton usage
    {
        private TrimInfixAndSuffixEncoder() { } // Singleton only

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static TrimInfixAndSuffixEncoder Instance { get; } = new TrimInfixAndSuffixEncoder();

        /// <summary>
        /// Maximum encodable single-byte code.
        /// </summary>
        private const int RemoveEverything = 255;


        /// <inheritdoc cref="ISequenceEncoder.PrefixBytes"/>
        public const int PrefixBytes = 3;

        int ISequenceEncoder.PrefixBytes => PrefixBytes;

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
            int maxInfixIndex = 0;
            int maxSubsequenceLength = BufferUtils.SharedPrefixLength(source, target);
            int maxInfixLength = 0;

            // There can be only two positions for the infix to delete:
            //
            // 1) We remove leading bytes, even if they are partially matching
            //    (but a longer match exists somewhere later on).
            //
            // 2) We leave the maximum matching prefix and remove non-matching
            //    bytes that follow.
            //
            // This follows the upstream implementation. The Java implementation
            // constructs a temporary sequence with the infix removed for each
            // candidate. We compare that virtual sequence directly instead,
            // avoiding a temporary buffer.
            for (int i = 0; i <= 1; i++)
            {
                int infixIndex = i == 0 ? 0 : maxSubsequenceLength;

                for (int infixLength = 1;
                    infixLength <= source.Length - infixIndex;
                    infixLength++)
                {
                    int sharedPrefix = BufferUtils.SharedPrefixLengthAfterRemoving(
                        source,
                        infixIndex,
                        infixLength,
                        target);

                    // Only update maxSubsequenceLength if we will be able
                    // to encode it.
                    if (sharedPrefix > 0 &&
                        sharedPrefix > maxSubsequenceLength &&
                        infixIndex < RemoveEverything &&
                        infixLength < RemoveEverything)
                    {
                        maxSubsequenceLength = sharedPrefix;
                        maxInfixIndex = infixIndex;
                        maxInfixLength = infixLength;
                    }
                }
            }

            int truncateSuffixBytes =
                source.Length - (maxInfixLength + maxSubsequenceLength);

            // Special case: if we're removing the suffix in the infix code,
            // move it to the suffix code instead.
            if (truncateSuffixBytes == 0 &&
                maxInfixIndex + maxInfixLength == source.Length)
            {
                truncateSuffixBytes = maxInfixLength;
                maxInfixIndex = 0;
                maxInfixLength = 0;
            }

            if (maxInfixIndex >= RemoveEverything ||
                maxInfixLength >= RemoveEverything ||
                truncateSuffixBytes >= RemoveEverything)
            {
                maxInfixIndex = 0;
                maxSubsequenceLength = 0;
                maxInfixLength = RemoveEverything;
                truncateSuffixBytes = RemoveEverything;
            }

            int suffixLength = target.Length - maxSubsequenceLength;
            int requiredLength = PrefixBytes + suffixLength;

            if (destination.Length < requiredLength)
            {
                bytesWritten = 0;
                return false;
            }

            destination[0] = (byte)(maxInfixIndex + 'A');
            destination[1] = (byte)(maxInfixLength + 'A');
            destination[2] = (byte)(truncateSuffixBytes + 'A');

            target.Slice(maxSubsequenceLength).CopyTo(destination.Slice(PrefixBytes));

            bytesWritten = requiredLength;
            return true;
        }

        /// <inheritdoc/>
        public bool TryDecode(ReadOnlySpan<byte> source, ReadOnlySpan<byte> encoded, Span<byte> destination, out int bytesWritten)
        {
            if (encoded.Length < PrefixBytes)
            {
                throw new ArgumentException("Encoded sequence must be at least 3 bytes long.", nameof(encoded));
            }

            int infixIndex = (encoded[0] - 'A') & 0xFF;
            int infixLength = (encoded[1] - 'A') & 0xFF;
            int truncateSuffixBytes = (encoded[2] - 'A') & 0xFF;

            if (infixLength == RemoveEverything ||
                truncateSuffixBytes == RemoveEverything)
            {
                infixIndex = 0;
                infixLength = source.Length;
                truncateSuffixBytes = 0;
            }
            else
            {
                if (infixIndex > source.Length)
                {
                    throw new ArgumentException("Encoded sequence specifies an infix position beyond the end of the source.", nameof(encoded));
                }

                if (infixLength > source.Length - infixIndex)
                {
                    throw new ArgumentException("Encoded sequence requests removal of more infix bytes than remain in the source.", nameof(encoded));
                }

                if (truncateSuffixBytes > source.Length - infixIndex - infixLength)
                {
                    throw new ArgumentException("Encoded sequence requests removal of more suffix bytes than remain after removing the infix.", nameof(encoded));
                }
            }

            int lengthWithoutInfix = source.Length - (infixIndex + infixLength + truncateSuffixBytes);
            int suffixLength = encoded.Length - 3;
            int requiredLength = checked(infixIndex + lengthWithoutInfix + suffixLength);

            if (destination.Length < requiredLength)
            {
                bytesWritten = 0;
                return false;
            }

            source.Slice(0, infixIndex).CopyTo(destination);

            source.Slice(
                infixIndex + infixLength,
                lengthWithoutInfix).CopyTo(
                    destination.Slice(infixIndex));

            encoded.Slice(3).CopyTo(
                destination.Slice(infixIndex + lengthWithoutInfix));

            bytesWritten = requiredLength;
            return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return nameof(TrimInfixAndSuffixEncoder);
        }
    }
}
