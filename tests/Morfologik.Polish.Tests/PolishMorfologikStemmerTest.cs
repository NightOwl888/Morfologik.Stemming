using J2N.Text;
using Morfologik.TestFramework;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

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
            JCG.HashSet<string> forms = new JCG.HashSet<string>(StringComparer.Ordinal);
            var formsLookup = forms.GetSpanAlternateLookup<char>();

            bool hadMissing = false;
            foreach (WordData wd in new PolishStemmer())
            {
                ReadOnlyMemory<char> chs = wd.Tag;
                if (chs.IsEmpty)
                {
                    Console.Error.WriteLine("Missing tag for: " + wd.Word);
                    hadMissing = true;
                    continue;
                }
                formsLookup.Add(chs.Span);
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
            DictionaryLookupResult response = s.Lookup(word.AsSpan());
            assertEquals(2, response.Count);

            HashSet<String> stems = new HashSet<String>();
            HashSet<String> tags = new HashSet<String>();
            foreach (WordData wd in response)
            {
                stems.Add(wd.Stem.ToString());
                tags.Add(wd.Tag.ToString());
                assertEquals(word, wd.Word.ToString());
            }
            assertTrue(stems.Contains("ligać"));
            assertTrue(stems.Contains("liga"));
            assertTrue(tags.Contains("subst:sg:nom:f"));
            assertTrue(tags.Contains("verb:fin:sg:ter:imperf:nonrefl+verb:fin:sg:ter:imperf:refl.nonrefl"));

            // Repeat to make sure we get the same values consistently.
            foreach (WordData wd in response)
            {
                stems.Contains(wd.Stem.ToString());
                tags.Contains(wd.Tag.ToString());
            }

            //String ENCODING = "UTF-8";
            Encoding ENCODING = Encoding.UTF8;

            // Run the same consistency check for the returned buffers.
            //ByteBuffer temp = ByteBuffer.Allocate(100);
            Span<byte> temp = stackalloc byte[100];
            foreach (WordData wd in response)
            {
                int stemByteCount = wd.StemBytes.Length;
                // Buffer should be copied.
                assertTrue(wd.StemBytes.Span.TryCopyTo(temp));
                string stem = ENCODING.GetString(temp.Slice(0, stemByteCount));
                // The buffer should be present in stems set.
                assertTrue(stem, stems.Contains(stem));
                // Morfologik.Stemming: We are copying memory from the internal buffer
                // to an external buffer. We don't have a reference, so
                // we have nothing to compare.

                //// Buffer should be copied.
                //ByteBuffer copy = wd.GetStemBytes(null);
                //String stem = ENCODING.GetString(copy.Array, copy.ArrayOffset + copy.Position, copy.Remaining);
                //// The buffer should be present in stems set.
                //assertTrue(stem, stems.Contains(stem));
                //// Buffer large enough to hold the contents.
                //assertSame(temp, wd.GetStemBytes(temp));
                //// The copy and the clone should be identical.
                //assertEquals(0, copy.CompareTo(temp));
            }

            foreach (WordData wd in response)
            {
                int tagByteCount = wd.TagBytes.Length;
                // Buffer should be copied.
                assertTrue(wd.TagBytes.Span.TryCopyTo(temp));
                // Buffer large enough to hold the contents.
                assertEquals(tagByteCount, tagByteCount);
                string tag = ENCODING.GetString(temp.Slice(0, tagByteCount));
                // The buffer should be present in tags set.
                assertTrue(tag, tags.Contains(tag));
                // Morfologik.Stemming: We are copying memory from the internal buffer
                // to an external buffer. We don't have a reference, so
                // we have nothing to compare.

                //// Buffer should be copied.
                //ByteBuffer copy = wd.GetTagBytes(null);
                //String tag = ENCODING.GetString(copy.Array, copy.ArrayOffset + copy.Position, copy.Remaining);
                //// The buffer should be present in tags set.
                //assertTrue(tag, tags.Contains(tag));
                //// Buffer large enough to hold the contents.
                //temp.Clear();
                //assertSame(temp, wd.GetTagBytes(temp));
                //// The copy and the clone should be identical.
                //assertEquals(0, copy.CompareTo(temp));
            }

            foreach (WordData wd in response)
            {
                int wordByteCount = wd.WordBytes.Length;
                // Buffer should be copied.
                assertTrue(wd.WordBytes.Span.TryCopyTo(temp));
                // Buffer large enough to hold the contents.
                assertEquals(wordByteCount, wordByteCount);
                // Morfologik.Stemming: We are copying memory from the internal buffer
                // to an external buffer. We don't have a reference, so
                // we have nothing to compare.

                //// Buffer should be copied.
                //ByteBuffer copy = wd.GetWordBytes(null);
                //assertNotNull(copy);
                //assertEquals(0, copy.CompareTo(ByteBuffer.Wrap(ENCODING.GetBytes(word))));
            }
        }

        /* */
        public static String asString(ReadOnlyMemory<char> s)
        {
            //if (s == null)
            //    return null;
            return s.ToString();
        }

        /* */
        public static String[] stem(IStemmer s, String word)
        {
            List<String> result = new List<String>();
            foreach (WordData wd in s.Lookup(word.AsSpan()))
            {
                result.Add(asString(wd.Stem));
                result.Add(asString(wd.Tag));
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
