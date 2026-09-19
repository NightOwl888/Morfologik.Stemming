using J2N.IO;
using Morfologik.TestFramework;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Morfologik.Stemming
{
    public class SequenceEncodersTest : TestCase
    {
        //private readonly ISequenceEncoder coder;

        //public SequenceEncodersTest(ISequenceEncoder coder)
        //{
        //    this.coder = coder;
        //}

        //@ParametersFactory
        //public static List<Object[]> testFactory()
        //{
        //    List<Object[]> encoders = new ArrayList<>();
        //    for (EncoderType t : EncoderType.values())
        //    {
        //        encoders.add(new Object[] { t.get() });
        //    }
        //    return encoders;
        //}

        private static class TestFactory
        {
            public static ISequenceEncoder[] Values =>
                ((EncoderType[])Enum.GetValues(typeof(EncoderType))).Select(x => x.Get()).ToArray();
        }

        [Test]
        public void TestEncodeSuffixOnRandomSequences([ValueSource(typeof(TestFactory), "Values")]ISequenceEncoder coder)
        {
            for (int i = 0; i < 10000; i++)
            {
                assertRoundtripEncode(
                    coder, 
                    randomAsciiLettersOfLengthBetween(0, 500),
                    randomAsciiLettersOfLengthBetween(0, 500));
            }
        }

        [Test]
        public void TestEncodeSamples([ValueSource(typeof(TestFactory), "Values")]ISequenceEncoder coder)
        {
            assertRoundtripEncode(coder, "", "");
            assertRoundtripEncode(coder, "abc", "ab");
            assertRoundtripEncode(coder, "abc", "abx");
            assertRoundtripEncode(coder, "ab", "abc");
            assertRoundtripEncode(coder, "xabc", "abc");
            assertRoundtripEncode(coder, "axbc", "abc");
            assertRoundtripEncode(coder, "axybc", "abc");
            assertRoundtripEncode(coder, "axybc", "abc");
            assertRoundtripEncode(coder, "azbc", "abcxy");

            assertRoundtripEncode(coder, "Niemcami", "Niemiec");
            assertRoundtripEncode(coder, "Niemiec", "Niemcami");
        }

        private void assertRoundtripEncode(ISequenceEncoder coder, string srcString, string dstString)
        {
            byte[] sourceBytes = Encoding.UTF8.GetBytes(srcString);
            byte[] targetBytes = Encoding.UTF8.GetBytes(dstString);

            ByteBuffer source = ByteBuffer.Wrap(sourceBytes);
            ByteBuffer target = ByteBuffer.Wrap(targetBytes);

            int maxEncodedByteCount =
                coder.GetMaxEncodedByteCount(
                    source.Remaining,
                    target.Remaining);

            byte[] encodedBytes = new byte[maxEncodedByteCount];

            if (!coder.TryEncode(
                source.Array.AsSpan(source.Position, source.Remaining),
                target.Array.AsSpan(target.Position, target.Remaining),
                encodedBytes,
                out int encodedByteCount))
            {
                throw new InvalidOperationException(
                    "The sequence encoder returned false even though the destination was sized using GetMaxEncodedByteCount().");
            }

            int maxDecodedByteCount =
                coder.GetMaxDecodedByteCount(
                    source.Remaining,
                    encodedByteCount);

            byte[] decodedBytes = new byte[maxDecodedByteCount];

            if (!coder.TryDecode(
                source.Array.AsSpan(source.Position, source.Remaining),
                encodedBytes.AsSpan(0, encodedByteCount),
                decodedBytes,
                out int decodedByteCount))
            {
                throw new InvalidOperationException(
                    "The sequence encoder returned false even though the destination was sized using GetMaxDecodedByteCount().");
            }

            if (!decodedBytes.AsSpan(0, decodedByteCount).SequenceEqual(targetBytes))
            {
                Console.Out.WriteLine("src: " + BufferUtils.ToString(source, Encoding.UTF8));
                Console.Out.WriteLine("dst: " + BufferUtils.ToString(target, Encoding.UTF8));
                Console.Out.WriteLine(
                    "enc: " + Encoding.UTF8.GetString(encodedBytes, 0, encodedByteCount));
                Console.Out.WriteLine(
                    "dec: " + Encoding.UTF8.GetString(decodedBytes, 0, decodedByteCount));

                Assert.Fail("Mismatch.");
            }
        }

        //private void assertRoundtripEncode(ISequenceEncoder coder, String srcString, String dstString)
        //{
        //    ByteBuffer source = ByteBuffer.Wrap(Encoding.UTF8.GetBytes(srcString));
        //    ByteBuffer target = ByteBuffer.Wrap(Encoding.UTF8.GetBytes(dstString));

        //    ByteBuffer encoded = coder.Encode(ByteBuffer.Allocate(Random.Next(30)), source, target);
        //    ByteBuffer decoded = coder.Decode(ByteBuffer.Allocate(Random.Next(30)), source, encoded);

        //    if (!decoded.Equals(target))
        //    {
        //        Console.Out.WriteLine("src: " + BufferUtils.ToString(source, Encoding.UTF8));
        //        Console.Out.WriteLine("dst: " + BufferUtils.ToString(target, Encoding.UTF8));
        //        Console.Out.WriteLine("enc: " + BufferUtils.ToString(encoded, Encoding.UTF8));
        //        Console.Out.WriteLine("dec: " + BufferUtils.ToString(decoded, Encoding.UTF8));
        //        fail("Mismatch.");
        //    }
        //}
    }
}
