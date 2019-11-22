using J2N.IO;
using System.Diagnostics;

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
    public class TrimPrefixAndSuffixEncoder : ISequenceEncoder
    {
        /// <summary>
        /// Maximum encodable single-byte code.
        /// </summary>
        private const int RemoveEverything = 255;

        /// <summary>
        /// 
        /// </summary>
        public virtual ByteBuffer Encode(ByteBuffer reuse, ByteBuffer source, ByteBuffer target)
        {
            // Search for the maximum matching subsequence that can be encoded. 
            int maxSubsequenceLength = 0;
            int maxSubsequenceIndex = 0;
            for (int i = 0; i < source.Remaining; i++)
            {
                // prefix at i => shared subsequence (infix)
                int sharedPrefix = BufferUtils.SharedPrefixLength(source, i, target, 0);
                // Only update maxSubsequenceLength if we will be able to encode it.
                if (sharedPrefix > maxSubsequenceLength && i < RemoveEverything
                    && (source.Remaining - (i + sharedPrefix)) < RemoveEverything)
                {
                    maxSubsequenceLength = sharedPrefix;
                    maxSubsequenceIndex = i;
                }
            }

            // Determine how much to remove (and where) from src to get a prefix of dst.
            int truncatePrefixBytes = maxSubsequenceIndex;
            int truncateSuffixBytes = (source.Remaining - (maxSubsequenceIndex + maxSubsequenceLength));
            if (truncatePrefixBytes >= RemoveEverything || truncateSuffixBytes >= RemoveEverything)
            {
                maxSubsequenceIndex = maxSubsequenceLength = 0;
                truncatePrefixBytes = truncateSuffixBytes = RemoveEverything;
            }

            int len1 = target.Remaining - maxSubsequenceLength;
            reuse = BufferUtils.ClearAndEnsureCapacity(reuse, 2 + len1);

            Debug.Assert(target.HasArray &&
                   target.Position == 0 &&
                   target.ArrayOffset == 0);

            reuse.Put((byte)((truncatePrefixBytes + 'A') & 0xFF));
            reuse.Put((byte)((truncateSuffixBytes + 'A') & 0xFF));
            reuse.Put(target.Array, maxSubsequenceLength, len1);
            reuse.Flip();

            return reuse;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual int PrefixBytes => 2;

        /// <summary>
        /// 
        /// </summary>
        public virtual ByteBuffer Decode(ByteBuffer reuse, ByteBuffer source, ByteBuffer encoded)
        {
            Debug.Assert(encoded.Remaining >= 2);

            int p = encoded.Position;
            int truncatePrefixBytes = (encoded.Get(p) - 'A') & 0xFF;
            int truncateSuffixBytes = (encoded.Get(p + 1) - 'A') & 0xFF;

            if (truncatePrefixBytes == RemoveEverything ||
                truncateSuffixBytes == RemoveEverything)
            {
                truncatePrefixBytes = source.Remaining;
                truncateSuffixBytes = 0;
            }

            Debug.Assert(source.HasArray &&
                   source.Position == 0 &&
                   source.ArrayOffset == 0);

            Debug.Assert(encoded.HasArray &&
                   encoded.Position == 0 &&
                   encoded.ArrayOffset == 0);

            int len1 = source.Remaining - (truncateSuffixBytes + truncatePrefixBytes);
            int len2 = encoded.Remaining - 2;
            reuse = BufferUtils.ClearAndEnsureCapacity(reuse, len1 + len2);

            reuse.Put(source.Array, truncatePrefixBytes, len1);
            reuse.Put(encoded.Array, 2, len2);
            reuse.Flip();

            return reuse;
        }

        // No need to override ToString() as it was only returning the type name, anyway
    }
}
