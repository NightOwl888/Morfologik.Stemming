// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Morfologik.TestFramework;
using NUnit.Framework;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Morfologik.Stemming
{
    public abstract class ArrayBufferWriterTests<T> where T : IEquatable<T>
    {
        [Test]
        public void ArrayBufferWriter_Ctor()
        {
            {
                var output = new ArrayBufferWriter<T>();
                Assert.AreEqual(0, output.FreeCapacity);
                Assert.AreEqual(0, output.Capacity);
                Assert.AreEqual(0, output.WrittenCount);
                Assert.IsTrue(ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan));
                Assert.IsTrue(ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            }

            {
                var output = new ArrayBufferWriter<T>(200);
                Assert.IsTrue(output.FreeCapacity >= 200);
                Assert.IsTrue(output.Capacity >= 200);
                Assert.AreEqual(0, output.WrittenCount);
                Assert.IsTrue(ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan));
                Assert.IsTrue(ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            }

            {
                ArrayBufferWriter<T> output = default;
                Assert.Null(output);
            }
        }

        [Test]
        //[ActiveIssue("https://github.com/mono/mono/issues/15002", TestRuntimes.Mono)]
        public void Invalid_Ctor()
        {
            Assert.Throws<ArgumentException>(() => new ArrayBufferWriter<T>(0));
            Assert.Throws<ArgumentException>(() => new ArrayBufferWriter<T>(-1));
            Assert.Throws<OutOfMemoryException>(() => new ArrayBufferWriter<T>(int.MaxValue));
        }

        [Test]
        public void Clear()
        {
            var output = new ArrayBufferWriter<T>(256);
            int previousAvailable = output.FreeCapacity;
            WriteData(output, 2);
            Assert.IsTrue(output.FreeCapacity < previousAvailable);
            Assert.IsTrue(output.WrittenCount > 0);
            Assert.False(ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan));
            Assert.False(ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.IsTrue(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));

            ReadOnlyMemory<T> transientMemory = output.WrittenMemory;
            ReadOnlySpan<T> transientSpan = output.WrittenSpan;
            T t0 = transientMemory.Span[0];
            T t1 = transientSpan[1];
            Assert.AreNotEqual(default, t0);
            Assert.AreNotEqual(default, t1);
            output.Clear();
            Assert.AreEqual(default(T), transientMemory.Span[0]);
            Assert.AreEqual(default(T), transientSpan[1]);

            Assert.AreEqual(0, output.WrittenCount);
            Assert.IsTrue(ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan));
            Assert.IsTrue(ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.AreEqual(previousAvailable, output.FreeCapacity);
        }

        [Test]
        public void ResetWrittenCount()
        {
            var output = new ArrayBufferWriter<T>(256);
            int previousAvailable = output.FreeCapacity;
            WriteData(output, 2);
            Assert.IsTrue(output.FreeCapacity < previousAvailable);
            Assert.IsTrue(output.WrittenCount > 0);
            Assert.False(ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan));
            Assert.False(ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.IsTrue(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));

            ReadOnlyMemory<T> transientMemory = output.WrittenMemory;
            ReadOnlySpan<T> transientSpan = output.WrittenSpan;
            T t0 = transientMemory.Span[0];
            T t1 = transientSpan[1];
            Assert.AreNotEqual(default(T), t0);
            Assert.AreNotEqual(default(T), t1);
            output.ResetWrittenCount();
            Assert.AreEqual(t0, transientMemory.Span[0]);
            Assert.AreEqual(t1, transientSpan[1]);

            Assert.AreEqual(0, output.WrittenCount);
            Assert.IsTrue(ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan));
            Assert.IsTrue(ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.AreEqual(previousAvailable, output.FreeCapacity);
        }

        [Test]
        public void Advance()
        {
            {
                var output = new ArrayBufferWriter<T>();
                int capacity = output.Capacity;
                Assert.AreEqual(capacity, output.FreeCapacity);
                output.Advance(output.FreeCapacity);
                Assert.AreEqual(capacity, output.WrittenCount);
                Assert.AreEqual(0, output.FreeCapacity);
            }

            {
                var output = new ArrayBufferWriter<T>();
                output.Advance(output.Capacity);
                Assert.AreEqual(output.Capacity, output.WrittenCount);
                Assert.AreEqual(0, output.FreeCapacity);
                int previousCapacity = output.Capacity;
                Span<T> _ = output.GetSpan();
                Assert.IsTrue(output.Capacity > previousCapacity);
            }

            {
                var output = new ArrayBufferWriter<T>(256);
                WriteData(output, 2);
                ReadOnlyMemory<T> previousMemory = output.WrittenMemory;
                ReadOnlySpan<T> previousSpan = output.WrittenSpan;
                Assert.IsTrue(previousSpan.SequenceEqual(previousMemory.Span));
                output.Advance(10);
                Assert.False(previousMemory.Span.SequenceEqual(output.WrittenMemory.Span));
                Assert.False(previousSpan.SequenceEqual(output.WrittenSpan));
                Assert.IsTrue(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));
            }

            {
                var output = new ArrayBufferWriter<T>();
                _ = output.GetSpan(20);
                WriteData(output, 10);
                ReadOnlyMemory<T> previousMemory = output.WrittenMemory;
                ReadOnlySpan<T> previousSpan = output.WrittenSpan;
                Assert.IsTrue(previousSpan.SequenceEqual(previousMemory.Span));
                Assert.Throws<InvalidOperationException>(() => output.Advance(247));
                output.Advance(10);
                Assert.False(previousMemory.Span.SequenceEqual(output.WrittenMemory.Span));
                Assert.False(previousSpan.SequenceEqual(output.WrittenSpan));
                Assert.IsTrue(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));
            }
        }

        [Test]
        public void AdvanceZero()
        {
            var output = new ArrayBufferWriter<T>();
            WriteData(output, 2);
            Assert.AreEqual(2, output.WrittenCount);
            ReadOnlyMemory<T> previousMemory = output.WrittenMemory;
            ReadOnlySpan<T> previousSpan = output.WrittenSpan;
            Assert.IsTrue(previousSpan.SequenceEqual(previousMemory.Span));
            output.Advance(0);
            Assert.AreEqual(2, output.WrittenCount);
            Assert.IsTrue(previousMemory.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.IsTrue(previousSpan.SequenceEqual(output.WrittenSpan));
            Assert.IsTrue(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));
        }

        [Test]
        public void InvalidAdvance()
        {
            {
                var output = new ArrayBufferWriter<T>();
                Assert.Throws<ArgumentException>(() => output.Advance(-1));
                Assert.Throws<InvalidOperationException>(() => output.Advance(output.Capacity + 1));
            }

            {
                var output = new ArrayBufferWriter<T>();
                WriteData(output, 100);
                Assert.Throws<InvalidOperationException>(() => output.Advance(output.FreeCapacity + 1));
            }
        }

        [Test]
        public void GetSpan_DefaultCtor()
        {
            var output = new ArrayBufferWriter<T>();
            Span<T> span = output.GetSpan();
            Assert.AreEqual(256, span.Length);
        }

        [TestCaseSource(nameof(SizeHints))]
        public void GetSpan_DefaultCtor_WithSizeHint(int sizeHint)
        {
            var output = new ArrayBufferWriter<T>();
            Span<T> span = output.GetSpan(sizeHint);
            Assert.AreEqual(sizeHint <= 256 ? 256 : sizeHint, span.Length);
        }

        [Test]
        public void GetSpan_InitSizeCtor()
        {
            var output = new ArrayBufferWriter<T>(100);
            Span<T> span = output.GetSpan();
            Assert.AreEqual(100, span.Length);
        }

        [TestCaseSource(nameof(SizeHints))]
        public void GetSpan_InitSizeCtor_WithSizeHint(int sizeHint)
        {
            {
                var output = new ArrayBufferWriter<T>(256);
                Span<T> span = output.GetSpan(sizeHint);
                Assert.AreEqual(sizeHint <= 256 ? 256 : sizeHint + 256, span.Length);
            }

            {
                var output = new ArrayBufferWriter<T>(1000);
                Span<T> span = output.GetSpan(sizeHint);
                Assert.AreEqual(sizeHint <= 1000 ? 1000 : sizeHint + 1000, span.Length);
            }
        }

        [Test]
        public void GetMemory_DefaultCtor()
        {
            var output = new ArrayBufferWriter<T>();
            Memory<T> memory = output.GetMemory();
            Assert.AreEqual(256, memory.Length);
        }

        [TestCaseSource(nameof(SizeHints))]
        public void GetMemory_DefaultCtor_WithSizeHint(int sizeHint)
        {
            var output = new ArrayBufferWriter<T>();
            Memory<T> memory = output.GetMemory(sizeHint);
            Assert.AreEqual(sizeHint <= 256 ? 256 : sizeHint, memory.Length);
        }

        [Test]
        public void GetMemory_ExceedMaximumBufferSize_WithSmallStartingSize()
        {
            var output = new ArrayBufferWriter<T>(256);
            Assert.Throws<OutOfMemoryException>(() => output.GetMemory(int.MaxValue));
        }

        [Test]
        public void GetMemory_InitSizeCtor()
        {
            var output = new ArrayBufferWriter<T>(100);
            Memory<T> memory = output.GetMemory();
            Assert.AreEqual(100, memory.Length);
        }

        [TestCaseSource(nameof(SizeHints))]
        public void GetMemory_InitSizeCtor_WithSizeHint(int sizeHint)
        {
            {
                var output = new ArrayBufferWriter<T>(256);
                Memory<T> memory = output.GetMemory(sizeHint);
                Assert.AreEqual(sizeHint <= 256 ? 256 : sizeHint + 256, memory.Length);
            }

            {
                var output = new ArrayBufferWriter<T>(1000);
                Memory<T> memory = output.GetMemory(sizeHint);
                Assert.AreEqual(sizeHint <= 1000 ? 1000 : sizeHint + 1000, memory.Length);
            }
        }

        //// NOTE: InvalidAdvance_Large test is constrained to run on Windows and MacOSX because it causes
        ////       problems on Linux due to the way deferred memory allocation works. On Linux, the allocation can
        ////       succeed even if there is not enough memory but then the test may get killed by the OOM killer at the
        ////       time the memory is accessed which triggers the full memory allocation.
        //[PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        //[ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))]
        //[OuterLoop]
        //public void InvalidAdvance_Large()
        //{
        //    try
        //    {
        //        {
        //            var output = new ArrayBufferWriter<T>(2_000_000_000);
        //            WriteData(output, 1_000);
        //            Assert.Throws<InvalidOperationException>(() => output.Advance(int.MaxValue));
        //            Assert.Throws<InvalidOperationException>(() => output.Advance(2_000_000_000 - 1_000 + 1));
        //        }
        //    }
        //    catch (OutOfMemoryException) { }
        //}

        [Test]
        public void GetMemoryAndSpan()
        {
            {
                var output = new ArrayBufferWriter<T>();
                WriteData(output, 2);
                Span<T> span = output.GetSpan();
                Memory<T> memory = output.GetMemory();
                Span<T> memorySpan = memory.Span;
                Assert.IsTrue(span.Length > 0);
                Assert.IsTrue(memorySpan.Length > 0);
                Assert.AreEqual(span.Length, memorySpan.Length);
                for (int i = 0; i < span.Length; i++)
                {
                    Assert.AreEqual(default(T), span[i]);
                    Assert.AreEqual(default(T), memorySpan[i]);
                }
            }

            {
                var output = new ArrayBufferWriter<T>();
                WriteData(output, 2);
                ReadOnlyMemory<T> writtenSoFarMemory = output.WrittenMemory;
                ReadOnlySpan<T> writtenSoFar = output.WrittenSpan;
                Assert.IsTrue(writtenSoFarMemory.Span.SequenceEqual(writtenSoFar));
                int previousAvailable = output.FreeCapacity;
                Span<T> span = output.GetSpan(500);
                Assert.IsTrue(span.Length >= 500);
                Assert.IsTrue(output.FreeCapacity >= 500);
                Assert.IsTrue(output.FreeCapacity > previousAvailable);

                Assert.AreEqual(writtenSoFar.Length, output.WrittenCount);
                Assert.False(writtenSoFar.SequenceEqual(span.Slice(0, output.WrittenCount)));

                Memory<T> memory = output.GetMemory();
                Span<T> memorySpan = memory.Span;
                Assert.IsTrue(span.Length >= 500);
                Assert.IsTrue(memorySpan.Length >= 500);
                Assert.AreEqual(span.Length, memorySpan.Length);
                for (int i = 0; i < span.Length; i++)
                {
                    Assert.AreEqual(default(T), span[i]);
                    Assert.AreEqual(default(T), memorySpan[i]);
                }

                memory = output.GetMemory(500);
                memorySpan = memory.Span;
                Assert.IsTrue(memorySpan.Length >= 500);
                Assert.AreEqual(span.Length, memorySpan.Length);
                for (int i = 0; i < memorySpan.Length; i++)
                {
                    Assert.AreEqual(default(T), memorySpan[i]);
                }
            }
        }

        [Test]
        public void GetSpanShouldAtleastDoubleWhenGrowing()
        {
            var output = new ArrayBufferWriter<T>(256);
            WriteData(output, 100);
            int previousAvailable = output.FreeCapacity;

            _ = output.GetSpan(previousAvailable);
            Assert.AreEqual(previousAvailable, output.FreeCapacity);

            _ = output.GetSpan(previousAvailable + 1);
            Assert.IsTrue(output.FreeCapacity >= previousAvailable * 2);
        }

        [Test]
        public void GetSpanOnlyGrowsAboveThreshold()
        {
            {
                var output = new ArrayBufferWriter<T>();
                _ = output.GetSpan();
                int previousAvailable = output.FreeCapacity;

                for (int i = 0; i < 10; i++)
                {
                    _ = output.GetSpan();
                    Assert.AreEqual(previousAvailable, output.FreeCapacity);
                }
            }

            {
                var output = new ArrayBufferWriter<T>();
                _ = output.GetSpan(10);
                int previousAvailable = output.FreeCapacity;

                for (int i = 0; i < 10; i++)
                {
                    _ = output.GetSpan(previousAvailable);
                    Assert.AreEqual(previousAvailable, output.FreeCapacity);
                }
            }
        }

        [Test]
        public void InvalidGetMemoryAndSpan()
        {
            var output = new ArrayBufferWriter<T>();
            WriteData(output, 2);
            AssertExtensions.Throws<ArgumentException>("sizeHint", () => output.GetSpan(-1));
            AssertExtensions.Throws<ArgumentException>("sizeHint", () => output.GetMemory(-1));
        }

        [Test]
        public void MultipleCallsToGetSpan()
        {
#if NET5_0_OR_GREATER
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
#else
            if (!typeof(T).IsValueType)
#endif
            {
                return;
            }

            var output = new ArrayBufferWriter<T>(300);
            Assert.IsTrue(MemoryMarshal.TryGetArray(output.GetMemory(), out ArraySegment<T> array));
            GCHandle pinnedArray = GCHandle.Alloc(array.Array, GCHandleType.Pinned);
            try
            {
                int previousAvailable = output.FreeCapacity;
                Assert.IsTrue(previousAvailable >= 300);
                Assert.IsTrue(output.Capacity >= 300);
                Assert.AreEqual(previousAvailable, output.Capacity);
                Span<T> span = output.GetSpan();
                Assert.IsTrue(span.Length >= previousAvailable);
                Assert.IsTrue(span.Length >= 256);
                Span<T> newSpan = output.GetSpan();
                Assert.AreEqual(span.Length, newSpan.Length);
                Assert.AreEqual((nint)0, Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(newSpan)));
                Assert.AreEqual(span.Length, output.GetSpan().Length);
            }
            finally
            {
                pinnedArray.Free();
            }
        }

        protected abstract void WriteData(IBufferWriter<T> bufferWriter, int numBytes);

        public static IEnumerable<object[]> SizeHints
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { 0 },
                    new object[] { 1 },
                    new object[] { 2 },
                    new object[] { 3 },
                    new object[] { 99 },
                    new object[] { 100 },
                    new object[] { 101 },
                    new object[] { 255 },
                    new object[] { 256 },
                    new object[] { 257 },
                    new object[] { 1000 },
                    new object[] { 2000 },
                };
            }
        }
    }
}
