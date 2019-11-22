using J2N.IO;

namespace Morfologik.Stemming
{
    /// <summary>
    /// No relative encoding at all (full target form is returned).
    /// </summary>
    public class NoEncoder : ISequenceEncoder
    {
        /// <summary>
        /// 
        /// </summary>
        public virtual ByteBuffer Encode(ByteBuffer reuse, ByteBuffer source, ByteBuffer target)
        {
            reuse = BufferUtils.ClearAndEnsureCapacity(reuse, target.Remaining);

            target.Mark();
            reuse.Put(target)
                 .Flip();
            target.Reset();

            return reuse;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual ByteBuffer Decode(ByteBuffer reuse, ByteBuffer source, ByteBuffer encoded)
        {
            reuse = BufferUtils.ClearAndEnsureCapacity(reuse, encoded.Remaining);

            encoded.Mark();
            reuse.Put(encoded)
                 .Flip();
            encoded.Reset();

            return reuse;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual int PrefixBytes => 0;

        // No need to override ToString() as it was only returning the type name, anyway
    }
}
