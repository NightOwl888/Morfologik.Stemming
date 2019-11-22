using Morfologik.TestFramework;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Morfologik.Stemming
{
    public class DictionaryMetadataTest : TestCase
    {
        [Test]
        public void TestEscapeSeparator()
        {
            DictionaryMetadata m = DictionaryMetadata.Read(GetType().getResourceAsStream("escape-separator.info"));
            //Assertions.assertThat(m.Separator).isEqualTo((byte) '\t');
            Assert.AreEqual((byte)'\t', m.Separator);
        }

        [Test]
        public void TestUnicodeSeparator()
        {
            DictionaryMetadata m = DictionaryMetadata.Read(GetType().getResourceAsStream("unicode-separator.info"));
            //Assertions.assertThat(m.getSeparator()).isEqualTo((byte) '\t');
            Assert.AreEqual((byte)'\t', m.Separator);
        }

        [Test]
        public void TestWriteMetadata()
        {
            StringWriter sw = new StringWriter();

            EncoderType encoder = randomFrom((EncoderType[])Enum.GetValues(typeof(EncoderType)));
            var encoding = randomFrom(new Encoding[] {
                Encoding.UTF8,
                Encoding.GetEncoding("iso-8859-1"),
                Encoding.ASCII
            });
            //Charset encoding = randomFrom(Arrays.asList(
            //    StandardCharsets.UTF_8,
            //    StandardCharsets.ISO_8859_1,
            //    StandardCharsets.US_ASCII));

            DictionaryMetadata.Builder()
              .Encoding(encoding)
              .Encoder(encoder)
              .Separator('|')
              .Build()
              .Write(sw);

            DictionaryMetadata other =
                DictionaryMetadata.Read(new MemoryStream(Encoding.UTF8.GetBytes(sw.ToString())));

            //Assertions.assertThat(other.getSeparator()).isEqualTo((byte) '|');
            //Assertions.assertThat(other.getDecoder().charset()).isEqualTo(encoding);
            //Assertions.assertThat(other.getEncoder().charset()).isEqualTo(encoding);
            //Assertions.assertThat(other.getSequenceEncoderType()).isEqualTo(encoder);
            Assert.AreEqual((byte)'|', other.Separator);
            Assert.AreEqual(encoding, other.Decoder);
            Assert.AreEqual(encoding, other.Encoder);
            Assert.AreEqual(encoder, other.SequenceEncoderType);
        }
    }
}
