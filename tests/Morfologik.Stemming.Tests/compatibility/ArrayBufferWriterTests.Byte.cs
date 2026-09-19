using NUnit.Framework;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Morfologik.Stemming
{
    public class ArrayBufferWriterTests_Byte : ArrayBufferWriterTests<byte>
    {
        protected override void WriteData(IBufferWriter<byte> bufferWriter, int numBytes)
        {
            Span<byte> outputSpan = bufferWriter.GetSpan(numBytes);
            Assert.True(outputSpan.Length >= numBytes);
            var random = new Random(42);

            var data = new byte[numBytes];
            random.NextBytes(data);
            data.CopyTo(outputSpan);

            bufferWriter.Advance(numBytes);
        }

        [Theory]
        [TestCase(true)]
        [TestCase(false)]
        public void WriteAndCopyToStream(bool clearContent)
        {
            ArrayBufferWriter<byte> output = new();
            WriteData(output, 100);

            using MemoryStream memStream = new(100);

            Assert.AreEqual(100, output.WrittenCount);

            ReadOnlySpan<byte> outputSpan = output.WrittenMemory.ToArray();

            ReadOnlyMemory<byte> transientMemory = output.WrittenMemory;
            ReadOnlySpan<byte> transientSpan = output.WrittenSpan;

            Assert.True(transientSpan.SequenceEqual(transientMemory.Span));

            Assert.True(transientSpan[0] != 0);
            byte expectedFirstByte = transientSpan[0];

            memStream.Write(transientSpan.ToArray(), 0, transientSpan.Length);

            if (clearContent)
            {
                expectedFirstByte = 0;
                output.Clear();
            }
            else
            {
                output.ResetWrittenCount();
            }

            Assert.AreEqual(expectedFirstByte, transientSpan[0]);
            Assert.AreEqual(expectedFirstByte, transientMemory.Span[0]);

            Assert.AreEqual(0, output.WrittenCount);
            byte[] streamOutput = memStream.ToArray();

            Assert.True(ReadOnlyMemory<byte>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.True(ReadOnlySpan<byte>.Empty.SequenceEqual(output.WrittenMemory.Span));
            Assert.True(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));

            Assert.AreEqual(outputSpan.Length, streamOutput.Length);
            Assert.True(outputSpan.SequenceEqual(streamOutput));
        }

        [Theory]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WriteAndCopyToStreamAsync(bool clearContent)
        {
            ArrayBufferWriter<byte> output = new();
            WriteData(output, 100);

            using MemoryStream memStream = new(100);

            Assert.AreEqual(100, output.WrittenCount);

            ReadOnlyMemory<byte> outputMemory = output.WrittenMemory.ToArray();

            ReadOnlyMemory<byte> transient = output.WrittenMemory;

            Assert.True(transient.Span[0] != 0);
            byte expectedFirstByte = transient.Span[0];

            await memStream.WriteAsync(transient.ToArray(), 0, transient.Length);

            if (clearContent)
            {
                expectedFirstByte = 0;
                output.Clear();
            }
            else
            {
                output.ResetWrittenCount();
            }

            Assert.True(transient.Span[0] == expectedFirstByte);

            Assert.AreEqual(0, output.WrittenCount);
            byte[] streamOutput = memStream.ToArray();

            Assert.True(ReadOnlyMemory<byte>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.True(ReadOnlySpan<byte>.Empty.SequenceEqual(output.WrittenMemory.Span));

            Assert.AreEqual(outputMemory.Length, streamOutput.Length);
            Assert.True(outputMemory.Span.SequenceEqual(streamOutput));
        }

        //// NOTE: GetMemory_ExceedMaximumBufferSize test is constrained to run on Windows and MacOSX because it causes
        ////       problems on Linux due to the way deferred memory allocation works. On Linux, the allocation can
        ////       succeed even if there is not enough memory but then the test may get killed by the OOM killer at the
        ////       time the memory is accessed which triggers the full memory allocation.
        //[PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        //[ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))]
        //[OuterLoop]
        //public void GetMemory_ExceedMaximumBufferSize()
        //{
        //    int initialCapacity = int.MaxValue / 2 + 1;

        //    try
        //    {
        //        var output = new ArrayBufferWriter<byte>(initialCapacity);
        //        output.Advance(initialCapacity);

        //        // Validate we can't double the buffer size, but can grow
        //        Memory<byte> memory;
        //        memory = output.GetMemory(1);

        //        // The buffer should grow more than the 1 byte requested otherwise performance will not be usable
        //        // between 1GB and 2GB. The current implementation maxes out the buffer size to Array.MaxLength.
        //        Assert.Equal(Array.MaxLength - initialCapacity, memory.Length);
        //        Assert.Throws<OutOfMemoryException>(() => output.GetMemory(int.MaxValue));
        //    }
        //    catch (OutOfMemoryException)
        //    {
        //        // On memory constrained devices, we can get an OutOfMemoryException, which we can safely ignore.
        //    }
        //}
    }
}
