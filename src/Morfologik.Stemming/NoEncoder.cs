using System;

namespace Morfologik.Stemming
{
    /// <summary>
    /// No relative encoding at all (full target form is returned).
    /// </summary>
    public sealed class NoEncoder : ISequenceEncoder // Morfologik.Stemming specific - marked sealed to prevent inheritance and ensure singleton usage
    {
        private NoEncoder() { } // Singleton only

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static NoEncoder Instance { get; } = new NoEncoder();


        /// <inheritdoc cref="ISequenceEncoder.PrefixBytes"/>
        public int PrefixBytes => 0;

        /// <inheritdoc/>
        public int GetMaxEncodedByteCount(int sourceByteCount, int targetByteCount)
        {
            return targetByteCount;
        }

        /// <inheritdoc/>
        public int GetMaxDecodedByteCount(int sourceByteCount, int encodedByteCount)
        {
            return encodedByteCount;
        }

        /// <inheritdoc/>
        public bool TryEncode(ReadOnlySpan<byte> source, ReadOnlySpan<byte> target, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < target.Length)
            {
                bytesWritten = 0;
                return false;
            }

            target.CopyTo(destination);
            bytesWritten = target.Length;
            return true;
        }

        /// <inheritdoc/>
        public bool TryDecode(ReadOnlySpan<byte> source, ReadOnlySpan<byte> encoded, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < encoded.Length)
            {
                bytesWritten = 0;
                return false;
            }

            encoded.CopyTo(destination);
            bytesWritten = encoded.Length;
            return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return nameof(NoEncoder);
        }
    }
}
