using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Morfologik.Fsa.Tests.Support
{
    public class StreamExtensionsTest
    {
#if !FEATURE_STREAM_READEXACTLY
        [Test]
        public void TestReadExactly_ZeroLength()
        {
            using var ms = new MemoryStream();
            Span<byte> buffer = Array.Empty<byte>();
            ms.ReadExactly(buffer); // should succeed
        }

        [Test]
        public void TestReadExactly_Success_FromStart()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            using var ms = new MemoryStream(bytes);

            Span<byte> buffer = stackalloc byte[2];
            ms.ReadExactly(buffer);

            Assert.AreEqual((byte)1, buffer[0]);
            Assert.AreEqual((byte)2, buffer[1]);
        }

        [Test]
        public void TestReadExactly_Success_FromMiddle()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            using var ms = new MemoryStream(bytes);
            ms.Seek(2, SeekOrigin.Begin);

            Span<byte> buffer = stackalloc byte[2];
            ms.ReadExactly(buffer);

            Assert.AreEqual((byte)3, buffer[0]);
            Assert.AreEqual((byte)4, buffer[1]);
        }

        [Test]
        public void TestReadExactly_Success_IntoMiddle()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            using var ms = new MemoryStream(bytes);

            Span<byte> buffer = stackalloc byte[4];
            ms.ReadExactly(buffer.Slice(2));

            Assert.AreEqual((byte)1, buffer[2]);
            Assert.AreEqual((byte)2, buffer[3]);
        }

        [Test]
        public void TestReadExactly_EndOfStream()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };

            Assert.Throws<EndOfStreamException>(() =>
            {
                using var ms = new MemoryStream(bytes);

                Span<byte> buffer = stackalloc byte[5];
                ms.ReadExactly(buffer);
            });
        }

        [Test]
        public void TestReadExactly_PartialReads()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var partialStream = new MaxBytesPerReadStream(bytes, maxBytesPerRead: 1);

            Span<byte> buffer = stackalloc byte[4];
            partialStream.ReadExactly(buffer);

            Assert.AreEqual((byte)1, buffer[0]);
            Assert.AreEqual((byte)2, buffer[1]);
            Assert.AreEqual((byte)3, buffer[2]);
            Assert.AreEqual((byte)4, buffer[3]);
        }

        /// <summary>
        /// A stream wrapper that returns at most <c>maxBytesPerRead</c> bytes per
        /// <see cref="ReadAsync(byte[], int, int, System.Threading.CancellationToken)"/> call,
        /// simulating partial reads (e.g. network streams).
        /// </summary>
        internal sealed class MaxBytesPerReadStream : MemoryStream
        {
            private readonly int maxBytesPerRead;

            public MaxBytesPerReadStream(byte[] data, int maxBytesPerRead)
                : base(data, writable: false)
            {
                this.maxBytesPerRead = maxBytesPerRead;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int clamped = Math.Min(count, maxBytesPerRead);
                return base.Read(buffer, offset, clamped);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
            {
                int clamped = Math.Min(count, maxBytesPerRead);
                return base.ReadAsync(buffer, offset, clamped, cancellationToken);
            }
        }
#endif

        [Test]
        public void TestReadByteRequired_Success()
        {
            var bytes = new byte[] { 42 };
            using var ms = new MemoryStream(bytes);

            byte value = ms.ReadByteRequired();

            Assert.AreEqual((byte)42, value);
        }

        [Test]
        public void TestReadByteRequired_EndOfStream()
        {
            using var ms = new MemoryStream();

            Assert.Throws<EndOfStreamException>(() =>
            {
                ms.ReadByteRequired();
            });
        }

        [Test]
        public void TestReadUInt16BigEndian_Success()
        {
            // 0x1234 in big-endian is bytes [0x12, 0x34] (4660 decimal)
            var bytes = new byte[] { 0x12, 0x34 };
            using var ms = new MemoryStream(bytes);

            ushort value = ms.ReadUInt16BigEndian();

            Assert.AreEqual(0x1234, value);
        }

        [Test]
        public void TestReadUInt16BigEndian_EndOfStream()
        {
            // Only 1 byte provided, but ReadUInt16BigEndian requires 2
            var bytes = new byte[] { 0x12 };
            using var ms = new MemoryStream(bytes);

            Assert.Throws<EndOfStreamException>(() =>
            {
                ms.ReadUInt16BigEndian();
            });
        }
    }
}
