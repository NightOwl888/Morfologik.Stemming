using J2N.IO;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Morfologik.Stemming
{
    [TestFixture]
    public class EncodersTest
    {
        [Test]
        public void TestSharedPrefix()
        {
            Assert.AreEqual(4, BufferUtils.SharedPrefixLength(
                  ByteBuffer.Wrap(b("abcdef")),
          ByteBuffer.Wrap(b("abcd__"))));

            Assert.AreEqual(0, BufferUtils.SharedPrefixLength(
                    ByteBuffer.Wrap(b("")),
                ByteBuffer.Wrap(b("_"))));

            Assert.AreEqual(2, BufferUtils.SharedPrefixLength(
                    ByteBuffer.Wrap(b("abcdef"), 2, 2),
                ByteBuffer.Wrap(b("___cd__"), 3, 2)));

      //      Assertions.assertThat(
      //        BufferUtils.SharedPrefixLength(
      //            ByteBuffer.Wrap(b("abcdef")),
      //    ByteBuffer.Wrap(b("abcd__"))))
      //.isEqualTo(4);

            //Assertions.assertThat(
            //    BufferUtils.SharedPrefixLength(
            //        ByteBuffer.Wrap(b("")),
            //    ByteBuffer.Wrap(b("_"))))
            //.isEqualTo(0);

            //Assertions.assertThat(
            //    BufferUtils.SharedPrefixLength(
            //        ByteBuffer.Wrap(b("abcdef"), 2, 2),
            //    ByteBuffer.Wrap(b("___cd__"), 3, 2)))
            //.isEqualTo(2);
        }

        private static byte[] b(string arg)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(arg);
            Assert.AreEqual(arg.Length, bytes.Length);
            //Assertions.assertThat(bytes).hasSize(arg.length());
            return bytes;
        }
    }
}
