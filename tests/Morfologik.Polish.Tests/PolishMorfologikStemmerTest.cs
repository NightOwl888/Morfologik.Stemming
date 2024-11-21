using J2N.IO;
using J2N.Text;
using Morfologik.TestFramework;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Morfologik.Stemming.Polish.Tests
{
    public class PolishMorfologikStemmerTest : TestCase
    {
        /* */
        [Test]
        public void TestLexemes()
        {
            PolishStemmer s = new PolishStemmer();

            assertEquals("żywotopisarstwo", stem(s, "żywotopisarstwie")[0]);
            assertEquals("abradować", stem(s, "abradowałoby")[0]);

            assertArrayEquals(new String[] { "żywotopisarstwo", "subst:sg:loc:n2" }, stem(s, "żywotopisarstwie"));
            assertArrayEquals(new String[] { "bazia", "subst:pl:inst:f" }, stem(s, "baziami"));

            // This word is not in the dictionary.
            assertNoStemFor(s, "martygalski");
        }

        /* */
        [Test]
        public void ListUniqueTags()
        {
            HashSet<String> forms = new HashSet<String>(StringComparer.Ordinal);
            bool hadMissing = false;
            foreach (WordData wd in new PolishStemmer())
            {
                ICharSequence chs = wd.GetTag();
                if (chs == null)
                {
                    Console.Error.WriteLine("Missing tag for: " + wd.Word);
                    hadMissing = true;
                    continue;
                }
                forms.Add(chs.ToString());
            }

            //Assertions.assertThat(hadMissing).isFalse();
            assertFalse(hadMissing);
        }

        /* */
        [Test]
        public void TestWordDataFields()
        {
            IStemmer s = new PolishStemmer();

            String word = "liga";
            IList<WordData> response = s.Lookup(word);
            assertEquals(2, response.Count);

            HashSet<String> stems = new HashSet<String>();
            HashSet<String> tags = new HashSet<String>();
            foreach (WordData wd in response)
            {
                stems.Add(wd.GetStem().ToString());
                tags.Add(wd.GetTag().ToString());
                assertEquals(word, wd.Word.ToString());
            }
            assertTrue(stems.Contains("ligać"));
            assertTrue(stems.Contains("liga"));
            assertTrue(tags.Contains("subst:sg:nom:f"));
            assertTrue(tags.Contains("verb:fin:sg:ter:imperf:nonrefl+verb:fin:sg:ter:imperf:refl.nonrefl"));

            // Repeat to make sure we get the same values consistently.
            foreach (WordData wd in response)
            {
                stems.Contains(wd.GetStem().ToString());
                tags.Contains(wd.GetTag().ToString());
            }

            //String ENCODING = "UTF-8";
            Encoding ENCODING = Encoding.UTF8;

            // Run the same consistency check for the returned buffers.
            ByteBuffer temp = ByteBuffer.Allocate(100);
            foreach (WordData wd in response)
            {
                // Buffer should be copied.
                ByteBuffer copy = wd.GetStemBytes(null);
                String stem = ENCODING.GetString(copy.Array, copy.ArrayOffset + copy.Position, copy.Remaining);
                // The buffer should be present in stems set.
                assertTrue(stem, stems.Contains(stem));
                // Buffer large enough to hold the contents.
                assertSame(temp, wd.GetStemBytes(temp));
                // The copy and the clone should be identical.
                assertEquals(0, copy.CompareTo(temp));
            }

            foreach (WordData wd in response)
            {
                // Buffer should be copied.
                ByteBuffer copy = wd.GetTagBytes(null);
                String tag = ENCODING.GetString(copy.Array, copy.ArrayOffset + copy.Position, copy.Remaining);
                // The buffer should be present in tags set.
                assertTrue(tag, tags.Contains(tag));
                // Buffer large enough to hold the contents.
                temp.Clear();
                assertSame(temp, wd.GetTagBytes(temp));
                // The copy and the clone should be identical.
                assertEquals(0, copy.CompareTo(temp));
            }

            foreach (WordData wd in response)
            {
                // Buffer should be copied.
                ByteBuffer copy = wd.GetWordBytes(null);
                assertNotNull(copy);
                assertEquals(0, copy.CompareTo(ByteBuffer.Wrap(ENCODING.GetBytes(word))));
            }
        }

        /* */
        public static String asString(ICharSequence s)
        {
            if (s == null)
                return null;
            return s.ToString();
        }

        /* */
        public static String[] stem(IStemmer s, String word)
        {
            List<String> result = new List<String>();
            foreach (WordData wd in s.Lookup(word))
            {
                result.Add(asString(wd.GetStem()));
                result.Add(asString(wd.GetTag()));
            }
            return result.ToArray();
        }

        /* */
        public static void assertNoStemFor(IStemmer s, String word)
        {
            assertArrayEquals(new String[] { }, stem(s, word));
        }
    }
}
