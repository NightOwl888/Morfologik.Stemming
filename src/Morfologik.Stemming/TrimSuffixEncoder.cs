using System;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Encodes <c>dst</c> relative to <c>src</c> by trimming whatever
    /// non-equal suffix <c>src</c> has. The output code is (bytes):
    /// 
    /// <code>
    /// {K}{suffix}
    /// </code>
    /// 
    /// where (<c>K</c> - 'A') bytes should be trimmed from the end of
    /// <c>src</c> and then the <c>suffix</c> should be appended to the
    /// resulting byte sequence.
    /// 
    /// <para>
    /// Examples:
    /// </para>
    /// 
    /// <code>
    /// src: foo
    /// dst: foobar
    /// encoded: Abar
    /// 
    /// src: foo
    /// dst: bar
    /// encoded: Dbar
    /// </code>
    /// </summary>
    public sealed class TrimSuffixEncoder : ISequenceEncoder // Morfologik.Stemming specific - marked sealed to prevent inheritance and ensure singleton usage
    {
        private TrimSuffixEncoder() { } // Singleton only

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static TrimSuffixEncoder Instance { get; } = new TrimSuffixEncoder();

        /// <summary>
        /// Maximum encodable single-byte code.
        /// </summary>
        private const int RemoveEverything = 255;


        /// <inheritdoc cref="ISequenceEncoder.PrefixBytes"/>
        public int PrefixBytes => 1;

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
            int sharedPrefixLength = BufferUtils.SharedPrefixLength(
                source,
                target);

            int truncateBytes = source.Length - sharedPrefixLength;

            if (truncateBytes >= RemoveEverything)
            {
                truncateBytes = RemoveEverything;
                sharedPrefixLength = 0;
            }

            int suffixLength = target.Length - sharedPrefixLength;
            int requiredLength = checked(PrefixBytes + suffixLength);

            if (destination.Length < requiredLength)
            {
                bytesWritten = 0;
                return false;
            }

            destination[0] = (byte)((truncateBytes + 'A') & 0xFF);

            target.Slice(sharedPrefixLength).CopyTo(destination.Slice(PrefixBytes));

            bytesWritten = requiredLength;
            return true;
        }

        /// <inheritdoc/>
        public bool TryDecode(ReadOnlySpan<byte> source, ReadOnlySpan<byte> encoded, Span<byte> destination, out int bytesWritten)
        {
            if (encoded.Length < PrefixBytes)
            {
                throw new ArgumentException("Encoded sequence must be at least 1 byte long.", nameof(encoded));
            }

            int truncateBytes = (encoded[0] - 'A') & 0xFF;

            if (truncateBytes == RemoveEverything)
            {
                truncateBytes = source.Length;
            }

            if (truncateBytes > source.Length)
            {
                throw new ArgumentException("Encoded sequence requests removal of more bytes than the source contains.", nameof(encoded));
            }

            int sourceLength = source.Length - truncateBytes;
            int suffixLength = encoded.Length - PrefixBytes;
            int requiredLength = checked(sourceLength + suffixLength);

            if (destination.Length < requiredLength)
            {
                bytesWritten = 0;
                return false;
            }

            source.Slice(0, sourceLength).CopyTo(destination);
            encoded.Slice(PrefixBytes).CopyTo(destination.Slice(sourceLength));

            bytesWritten = requiredLength;
            return true;
        }


        /// <inheritdoc/>
        public override string ToString()
        {
            return nameof(TrimSuffixEncoder);
        }
    }
}
