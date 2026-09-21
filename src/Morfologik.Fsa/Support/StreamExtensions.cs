using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;

namespace Morfologik.Fsa
{
    /// <summary>
    /// Extensions to <see cref="Stream"/>.
    /// </summary>
    internal static class StreamExtensions
    {
#if !FEATURE_STREAM_READEXACTLY
        public static void ReadExactly(this Stream stream, Span<byte> buffer)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

                        byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                while (buffer.Length > 0)
                {
                    int numRead = stream.Read(sharedBuffer, 0, buffer.Length);

                    if (numRead == 0)
                    {
                        throw new EndOfStreamException();
                    }

                    new ReadOnlySpan<byte>(sharedBuffer, 0, numRead).CopyTo(buffer);
                    buffer = buffer.Slice(numRead);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }
#endif

        public static ushort ReadUInt16BigEndian(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[2];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        public static byte ReadByteRequired(this Stream stream)
        {
            int value = stream.ReadByte();
            if (value < 0)
                throw new EndOfStreamException();

            return (byte)value;
        }
    }
}
