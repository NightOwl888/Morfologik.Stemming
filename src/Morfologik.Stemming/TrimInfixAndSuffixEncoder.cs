using J2N.IO;
using System.Diagnostics;

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
    public class TrimInfixAndSuffixEncoder : ISequenceEncoder
    {
        /// <summary>
        /// Maximum encodable single-byte code.
        /// </summary>
        private const int RemoveEverything = 255;
        private ByteBuffer scratch = ByteBuffer.Allocate(0);

        /// <summary>
        /// 
        /// </summary>
        public virtual ByteBuffer Encode(ByteBuffer reuse, ByteBuffer source, ByteBuffer target)
        {
            Debug.Assert(source.HasArray &&
                   source.Position == 0 &&
                   source.ArrayOffset == 0);

            Debug.Assert(target.HasArray &&
                   target.Position == 0 &&
                   target.ArrayOffset == 0);

            // Search for the infix that can we can encode and remove from src
            // to get a maximum-length prefix of dst. This could be done more efficiently
            // by running a smarter longest-common-subsequence algorithm and some pruning (?).
            //
            // For now, naive loop should do.

            // There can be only two positions for the infix to delete:
            // 1) we remove leading bytes, even if they are partially matching (but a longer match
            //    exists somewhere later on).
            // 2) we leave max. matching prefix and remove non-matching bytes that follow. 
            int maxInfixIndex = 0;
            int maxSubsequenceLength = BufferUtils.SharedPrefixLength(source, target);
            int maxInfixLength = 0;
            foreach (int i in new int[] { 0, maxSubsequenceLength })
            {
                for (int j = 1; j <= source.Remaining - i; j++)
                {
                    // Compute temporary src with the infix removed.
                    // Concatenate in scratch space for simplicity.
                    int len2 = source.Remaining - (i + j);
                    scratch = BufferUtils.ClearAndEnsureCapacity(scratch, i + len2);
                    scratch.Put(source.Array, 0, i);
                    scratch.Put(source.Array, i + j, len2);
                    scratch.Flip();

                    int sharedPrefix = BufferUtils.SharedPrefixLength(scratch, target);

                    // Only update maxSubsequenceLength if we will be able to encode it.
                    if (sharedPrefix > 0 && sharedPrefix > maxSubsequenceLength && i < RemoveEverything && j < RemoveEverything)
                    {
                        maxSubsequenceLength = sharedPrefix;
                        maxInfixIndex = i;
                        maxInfixLength = j;
                    }
                }
            }

            int truncateSuffixBytes = source.Remaining - (maxInfixLength + maxSubsequenceLength);

            // Special case: if we're removing the suffix in the infix code, move it
            // to the suffix code instead.
            if (truncateSuffixBytes == 0 &&
                maxInfixIndex + maxInfixLength == source.Remaining)
            {
                truncateSuffixBytes = maxInfixLength;
                maxInfixIndex = maxInfixLength = 0;
            }

            if (maxInfixIndex >= RemoveEverything ||
                maxInfixLength >= RemoveEverything ||
                truncateSuffixBytes >= RemoveEverything)
            {
                maxInfixIndex = maxSubsequenceLength = 0;
                maxInfixLength = truncateSuffixBytes = RemoveEverything;
            }

            int len1 = target.Remaining - maxSubsequenceLength;
            reuse = BufferUtils.ClearAndEnsureCapacity(reuse, 3 + len1);

            reuse.Put((byte)((maxInfixIndex + 'A') & 0xFF));
            reuse.Put((byte)((maxInfixLength + 'A') & 0xFF));
            reuse.Put((byte)((truncateSuffixBytes + 'A') & 0xFF));
            reuse.Put(target.Array, maxSubsequenceLength, len1);
            reuse.Flip();

            return reuse;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual int PrefixBytes => 3;


        /// <summary>
        /// 
        /// </summary>
        public virtual ByteBuffer Decode(ByteBuffer reuse, ByteBuffer source, ByteBuffer encoded)
        {
            Debug.Assert(encoded.Remaining >= 3);

            int p = encoded.Position;
            int infixIndex = (encoded.Get(p) - 'A') & 0xFF;
            int infixLength = (encoded.Get(p + 1) - 'A') & 0xFF;
            int truncateSuffixBytes = (encoded.Get(p + 2) - 'A') & 0xFF;

            if (infixLength == RemoveEverything ||
                truncateSuffixBytes == RemoveEverything)
            {
                infixIndex = 0;
                infixLength = source.Remaining;
                truncateSuffixBytes = 0;
            }

            int len1 = source.Remaining - (infixIndex + infixLength + truncateSuffixBytes);
            int len2 = encoded.Remaining - 3;
            reuse = BufferUtils.ClearAndEnsureCapacity(reuse, infixIndex + len1 + len2);

            Debug.Assert(encoded.HasArray &&
                   encoded.Position == 0 &&
                   encoded.ArrayOffset == 0);

            Debug.Assert(source.HasArray &&
                   source.Position == 0 &&
                   source.ArrayOffset == 0);

            reuse.Put(source.Array, 0, infixIndex);
            reuse.Put(source.Array, infixIndex + infixLength, len1);
            reuse.Put(encoded.Array, 3, len2);
            reuse.Flip();

            return reuse;
        }

        // No need to override ToString() as it was only returning the type name, anyway
    }
}
