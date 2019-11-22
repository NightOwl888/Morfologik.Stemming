using J2N.IO;
using System.Diagnostics;

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
    public class TrimSuffixEncoder : ISequenceEncoder
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
            int sharedPrefix = BufferUtils.SharedPrefixLength(source, target);
            int truncateBytes = source.Remaining - sharedPrefix;
            if (truncateBytes >= RemoveEverything)
            {
                truncateBytes = RemoveEverything;
                sharedPrefix = 0;
            }

            reuse = BufferUtils.ClearAndEnsureCapacity(reuse, 1 + target.Remaining - sharedPrefix);

            Debug.Assert(target.HasArray &&
                   target.Position == 0 &&
                   target.ArrayOffset == 0);

            byte suffixTrimCode = (byte)(truncateBytes + 'A');
            reuse.Put(suffixTrimCode)
                 .Put(target.Array, sharedPrefix, target.Remaining - sharedPrefix)
                 .Flip();

            return reuse;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual int PrefixBytes => 1;


        /// <summary>
        /// 
        /// </summary>
        public virtual ByteBuffer Decode(ByteBuffer reuse, ByteBuffer source, ByteBuffer encoded)
        {
            Debug.Assert(encoded.Remaining >= 1);

            int suffixTrimCode = encoded.Get(encoded.Position);
            int truncateBytes = (suffixTrimCode - 'A') & 0xFF;
            if (truncateBytes == RemoveEverything)
            {
                truncateBytes = source.Remaining;
            }

            int len1 = source.Remaining - truncateBytes;
            int len2 = encoded.Remaining - 1;

            reuse = BufferUtils.ClearAndEnsureCapacity(reuse, len1 + len2);

            Debug.Assert(source.HasArray &&
                   source.Position == 0 &&
                   source.ArrayOffset == 0);

            Debug.Assert(encoded.HasArray &&
                   encoded.Position == 0 &&
                   encoded.ArrayOffset == 0);

            reuse.Put(source.Array, 0, len1)
                 .Put(encoded.Array, 1, len2)
                 .Flip();

            return reuse;
        }

        // No need to override ToString() as it was only returning the type name, anyway
    }
}
