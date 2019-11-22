using Lucene.Net.Support;
using Morfologik.Fsa;
using Morfologik.TestFramework;
//using Morfologik.TestFramework.Http;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Morfologik.Stemming
{
    public class DictionaryLookupTest : TestCase
    {
        [Test]
        public void TestApplyReplacements()
        {
            LinkedHashMap<String, String> conversion = new LinkedHashMap<string, string>();
            conversion["'"] = "`";
            conversion["fi"] = "ﬁ";
            conversion["\\a"] = "ą";
            conversion["Barack"] = "George";
            conversion["_"] = "xx";
            assertEquals("ﬁlut", DictionaryLookup.ApplyReplacements("filut", conversion));
            assertEquals("ﬁzdrygałką", DictionaryLookup.ApplyReplacements("fizdrygałk\\a", conversion));
            assertEquals("George Bush", DictionaryLookup.ApplyReplacements("Barack Bush", conversion));
            assertEquals("xxxxxxxx", DictionaryLookup.ApplyReplacements("____", conversion));
        }

        /// <summary>
        /// Reads both the metadata and fsa into a <see cref="Dictionary"/>.
        /// </summary>
        private Dictionary ReadDictionary(string dictionaryName)
        {
            using (var fsaStream = this.GetType().getResourceAsStream(dictionaryName))
            using (var metadataStream = this.GetType().getResourceAsStream(DictionaryMetadata.GetExpectedMetadataFileName(dictionaryName)))
                return Dictionary.Read(fsaStream, metadataStream);
        }

        [Test]
        public void TestRemovedEncoderProperties()
        {
            //URL url = this.GetType().getResource("test-removed-props.dict");
            string dict = "test-removed-props.dict";
            try
            {
                new DictionaryLookup(ReadDictionary(dict));
                fail();
            }
            catch (IOException e)
            {
                //assertThat(e).hasMessageContaining(
                //    DictionaryAttribute.Encoder.GetInstance().PropertyName);

                assertTrue(e.Message.Contains(DictionaryAttributeExtensions.Encoder.PropertyName));
            }
        }

        [Test]
        public void TestPrefixDictionaries()
        {
            //URL url = this.GetType().getResource("test-prefix.dict");
            string dict = "test-prefix.dict";
            IStemmer s = new DictionaryLookup(ReadDictionary(dict));

            assertArrayEquals(new String[] { "Rzeczpospolita", "subst:irreg" },
                    stem(s, "Rzeczypospolitej"));
            assertArrayEquals(new String[] { "Rzeczpospolita", "subst:irreg" },
                stem(s, "Rzecząpospolitą"));

            // This word is not in the dictionary.
            assertNoStemFor(s, "martygalski");
        }

        [Test]
        public void TestInputConversion()
        {
            //URL url = this.GetType().getResource("test-prefix.dict");

            string dict = "test-prefix.dict";
            IStemmer s = new DictionaryLookup(ReadDictionary(dict));

            assertArrayEquals(new String[] { "Rzeczpospolita", "subst:irreg" },
                    stem(s, "Rzecz\\apospolit\\a"));

            assertArrayEquals(new String[] { "Rzeczpospolita", "subst:irreg" },
                stem(s, "krowa\\apospolit\\a"));
        }

        /* */
        [Test]
        public void TestInfixDictionaries()
        {
            //URL url = this.GetType().getResource("test-infix.dict");

            string dict = "test-infix.dict";
            IStemmer s = new DictionaryLookup(ReadDictionary(dict));

            //Assertions.assertThat(stem(s, "Rzeczypospolitej"))
            //      .containsExactly("Rzeczpospolita", "subst:irreg");

            //Assertions.assertThat(stem(s, "Rzeczyccy"))
            //      .containsExactly("Rzeczycki", "adj:pl:nom:m");

            //Assertions.assertThat(stem(s, "Rzecząpospolitą"))
            //      .containsExactly("Rzeczpospolita", "subst:irreg");

            assertEquals(new string[] { "Rzeczpospolita", "subst:irreg" }, stem(s, "Rzeczypospolitej"));
            assertEquals(new string[] { "Rzeczycki", "adj:pl:nom:m" }, stem(s, "Rzeczyccy"));
            assertEquals(new string[] { "Rzeczpospolita", "subst:irreg" }, stem(s, "Rzecząpospolitą"));

            // This word is not in the dictionary.
            assertNoStemFor(s, "martygalski");

            // This word uses characters that are outside of the encoding range of the dictionary. 
            assertNoStemFor(s, "Rzeczyckiõh");
        }

        /* */
        [Test]
        public void TestWordDataIterator()
        {
            //URL url = this.GetType().getResource("test-infix.dict");

            string dict = "test-infix.dict";
            DictionaryLookup s = new DictionaryLookup(ReadDictionary(dict));

            HashSet<String> entries = new HashSet<String>();
            foreach (WordData wd in s)
            {
                entries.Add(wd.Word + " " + wd.GetStem() + " " + wd.GetTag());
            }

            // Make sure a sample of the entries is present.
            //Assertions.assertThat(entries)
            //  .contains(
            //      "Rzekunia Rzekuń subst:sg:gen:m",
            //      "Rzeczkowskie Rzeczkowski adj:sg:nom.acc.voc:n+adj:pl:acc.nom.voc:f.n",
            //      "Rzecząpospolitą Rzeczpospolita subst:irreg",
            //      "Rzeczypospolita Rzeczpospolita subst:irreg",
            //      "Rzeczypospolitych Rzeczpospolita subst:irreg",
            //      "Rzeczyckiej Rzeczycki adj:sg:gen.dat.loc:f");

            assertTrue(entries.IsSupersetOf(new string[]
            {
            "Rzekunia Rzekuń subst:sg:gen:m",
                "Rzeczkowskie Rzeczkowski adj:sg:nom.acc.voc:n+adj:pl:acc.nom.voc:f.n",
                "Rzecząpospolitą Rzeczpospolita subst:irreg",
                "Rzeczypospolita Rzeczpospolita subst:irreg",
                "Rzeczypospolitych Rzeczpospolita subst:irreg",
                "Rzeczyckiej Rzeczycki adj:sg:gen.dat.loc:f"
            }));
        }

        /* */
        [Test]
        public void TestWordDataCloning()
        {
            //URL url = this.GetType().getResource("test-infix.dict");

            string dict = "test-infix.dict";


            DictionaryLookup s = new DictionaryLookup(ReadDictionary(dict));

            List<WordData> words = new List<WordData>();
            foreach (WordData wd in s)
            {
                WordData clone = (WordData)wd.Clone();
                words.Add(clone);
            }

            // Reiterate and verify that we have the same entries.
            DictionaryLookup s2 = new DictionaryLookup(ReadDictionary(dict));
            int i = 0;
            foreach (WordData wd in s2)
            {
                WordData clone = words[i++];
                assertEquals(clone.GetStem(), wd.GetStem());
                assertEquals(clone.GetTag(), wd.GetTag());
                assertEquals(clone.Word, wd.Word);
            }

            // Check collections contract.
            HashSet<WordData> entries = new HashSet<WordData>();
            try
            {
                entries.Add(words[0]);
                fail();
            }
            catch (Exception e)
            {
                // Expected.
            }
        }

        //private void assertEqualSequences(J2N.Text.ICharSequence s1, J2N.Text.ICharSequence s2)
        //{
        //    assertEquals(s1.ToString(), s2.ToString());
        //}

        /* */
        [Test]
        public void TestMultibyteEncodingUTF8()
        {
            //URL url = this.GetType().getResource("test-diacritics-utf8.dict");
            string dict = "test-diacritics-utf8.dict";
            Dictionary read = ReadDictionary(dict);
            IStemmer s = new DictionaryLookup(read);

            assertArrayEquals(new String[] { "merge", "001" }, stem(s, "mergeam"));
            assertArrayEquals(new String[] { "merge", "002" }, stem(s, "merseserăm"));
        }

        /* */
        [Test]
        public void TestSynthesis()
        {
            //URL url = this.GetType().getResource("test-synth.dict");
            string dict = "test-synth.dict";
            IStemmer s = new DictionaryLookup(ReadDictionary(dict));

            assertArrayEquals(new String[] { "miała", null }, stem(s,
                    "mieć|verb:praet:sg:ter:f:?perf"));
            assertArrayEquals(new String[] { "a", null }, stem(s, "a|conj"));
            assertArrayEquals(new String[] { }, stem(s, "dziecko|subst:sg:dat:n"));

            // This word is not in the dictionary.
            assertNoStemFor(s, "martygalski");
        }

        /* */
        [Test]
        public void TestInputWithSeparators()
        {
            //URL url = this.GetType().getResource("test-separators.dict");
            string dict = "test-separators.dict";
            DictionaryLookup s = new DictionaryLookup(ReadDictionary(dict));

            /*
             * Attemp to reconstruct input sequences using WordData iterator.
             */
            List<String> sequences = new List<String>();
            foreach (WordData wd in s)
            {
                var stemSequence = wd.GetStem();
                var tagSequence = wd.GetTag();
                var stem = stemSequence == null ? "null" : stemSequence.ToString();
                var tag = tagSequence == null ? "null" : tagSequence.ToString();

                sequences.Add($"{wd.Word} {stem} {tag}");
            }
            sequences.Sort(StringComparer.Ordinal);
            
            assertEquals("token1 null null", sequences[0]);
            assertEquals("token2 null null", sequences[1]);
            assertEquals("token3 null +", sequences[2]);
            assertEquals("token4 token2 null", sequences[3]);
            assertEquals("token5 token2 null", sequences[4]);
            assertEquals("token6 token2 +", sequences[5]);
            assertEquals("token7 token2 token3+", sequences[6]);
            assertEquals("token8 token2 token3++", sequences[7]);
        }

        /* */
        [Test]
        public void TestSeparatorInLookupTerm()
        {
            FSA fsa = FSA.Read(GetType().getResourceAsStream("test-separator-in-lookup.fsa"));

            DictionaryMetadata metadata = new DictionaryMetadataBuilder()
              .Separator('+')
              .Encoding("iso8859-1")
              .Encoder(EncoderType.Infix)
              .Build();

            DictionaryLookup s = new DictionaryLookup(new Dictionary(fsa, metadata));
            assertEquals(0, s.Lookup("l+A").Count);
        }

        /* */
        [Test]
        public void TestGetSeparator()
        {
            //URL url = this.GetType().getResource("test-separators.dict");
            string dict = "test-separators.dict";
            DictionaryLookup s = new DictionaryLookup(ReadDictionary(dict));
            assertEquals('+', s.SeparatorChar);
        }

        /* */
        public static String asString(J2N.Text.ICharSequence s)
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
