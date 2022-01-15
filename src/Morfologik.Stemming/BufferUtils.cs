using J2N.IO;
using System;
using System.Text;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Utilities to setup buffers.
    /// </summary>
    public static class BufferUtils
    {
        /// <summary>
        /// Ensure the buffer's capacity is large enough to hold a given number
        /// of elements. If the input buffer is not large enough, a new buffer is allocated
        /// and returned.
        /// </summary>
        /// <param name="buffer">The buffer to check or <c>null</c> if a new buffer should be allocated.</param>
        /// <param name="elements">The required number of elements to be appended to the buffer.</param>
        /// <returns>Returns the same buffer or a new buffer with the given capacity.</returns>
        public static ByteBuffer ClearAndEnsureCapacity(ByteBuffer? buffer, int elements)
        {
            if (buffer is null || buffer.Capacity < elements)
            {
                buffer = ByteBuffer.Allocate(elements);
            }
            else
            {
                buffer.Clear();
            }
            return buffer;
        }

        /// <summary>
        /// Ensure the buffer's capacity is large enough to hold a given number
        /// of elements. If the input buffer is not large enough, a new buffer is allocated
        /// and returned.
        /// </summary>
        /// <param name="buffer">The buffer to check or <c>null</c> if a new buffer should be allocated.</param>
        /// <param name="elements">The required number of elements to be appended to the buffer.</param>
        /// <returns>Returns the same buffer or a new buffer with the given capacity.</returns>
        public static CharBuffer ClearAndEnsureCapacity(CharBuffer buffer, int elements)
        {
            if (buffer == null || buffer.Capacity < elements)
            {
                buffer = CharBuffer.Allocate(elements);
            }
            else
            {
                buffer.Clear();
            }
            return buffer;
        }

        /// <summary>
        /// Converts bytes to a string using the specified encoding.
        /// </summary>
        /// <param name="buffer">The buffer to convert to a string.</param>
        /// <param name="encoding">The charset to use when converting bytes to characters.</param>
        /// <returns>A string representation of buffer's content.</returns>
        public static string ToString(ByteBuffer buffer, Encoding encoding)
        {
            buffer = buffer.Slice();
            byte[] buf = new byte[buffer.Remaining];
            buffer.Get(buf);
            return encoding.GetString(buf);
        }

        /// <summary>
        /// Converts chars into a string.
        /// </summary>
        /// <param name="buffer">The buffer to convert to a string.</param>
        /// <returns>A string representation of buffer's content.</returns>
        public static string ToString(CharBuffer buffer)
        {
            buffer = buffer.Slice();
            char[] buf = new char[buffer.Remaining];
            buffer.Get(buf);
            return new string(buf);
        }

        /// <summary>
        /// Returns the remaining bytes from the buffer copied to an array.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <returns>Returns the remaining bytes from the buffer copied to an array.</returns>
        public static byte[] ToArray(ByteBuffer buffer)
        {
            byte[] dst = new byte[buffer.Remaining];
            buffer.Mark();
            buffer.Get(dst);
            buffer.Reset();
            return dst;
        }

        /// <summary>
        /// Compute the length of the shared prefix between two byte sequences.
        /// </summary>
        internal static int SharedPrefixLength(ByteBuffer a, int aStart, ByteBuffer b, int bStart)
        {
            int i = 0;
            int max = Math.Min(a.Remaining - aStart, b.Remaining - bStart);
            aStart += a.Position;
            bStart += b.Position;
            while (i < max && a.Get(aStart++) == b.Get(bStart++))
            {
                i++;
            }
            return i;
        }

        /// <summary>
        /// Compute the length of the shared prefix between two byte sequences.
        /// </summary>
        internal static int SharedPrefixLength(ByteBuffer a, ByteBuffer b)
        {
            return SharedPrefixLength(a, 0, b, 0);
        }

        /// <summary>
        /// Convert byte buffer's content into characters. The input buffer's bytes are not
        /// consumed (mark is set and reset).
        /// </summary>
        public static CharBuffer BytesToChars(Encoding decoder, ByteBuffer bytes, CharBuffer chars)
        {
            //Debug.Assert( decoder.malformedInputAction() == CodingErrorAction.REPORT;

            chars = ClearAndEnsureCapacity(chars, decoder.GetMaxCharCount(bytes.Remaining));

            bytes.Mark();

            byte[] b = ToArray(bytes);
            string decoded = decoder.GetString(b);
            chars.Put(decoded);

            chars.Flip();
            bytes.Reset();

            return chars;
        }

        /// <summary>
        /// Convert chars into bytes.
        /// </summary>
        public static ByteBuffer CharsToBytes(Encoding encoder, CharBuffer chars, ByteBuffer bytes)
        {
            //assert encoder.malformedInputAction() == CodingErrorAction.REPORT;

            bytes = ClearAndEnsureCapacity(bytes, encoder.GetMaxByteCount(chars.Remaining));

            chars.Mark();

            var temp = chars.ToString();
            byte[] b = encoder.GetBytes(temp);
            bytes.Put(b);

            bytes.Flip();
            chars.Reset();

            return bytes;
        }
    }
}
