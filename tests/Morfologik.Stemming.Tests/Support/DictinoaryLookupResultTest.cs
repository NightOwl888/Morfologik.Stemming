using Morfologik.TestFramework;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Morfologik.Stemming
{
    public class DictionaryLookupResultTest : TestCase
    {
        private Dictionary ReadDictionary(string dictionaryName)
        {
            using (var fsaStream = this.GetType().getResourceAsStream(dictionaryName))
            using (var metadataStream = this.GetType().getResourceAsStream(DictionaryMetadata.GetExpectedMetadataFileName(dictionaryName)))
                return Dictionary.Read(fsaStream, metadataStream);
        }

        [Test]
        public void TestLookupResultReuseAndCount()
        {
            string dict = "test-prefix.dict";
            var lookup = new DictionaryLookup(ReadDictionary(dict));

            // Initial lookup creates a new result or populates a reused one
            DictionaryLookupResult result = lookup.Lookup("Rzeczypospolitej".AsSpan());
            int initialCount = result.Count;
            assertTrue(initialCount > 0);
            assertEquals("Rzeczypospolitej", result.Word.ToString());

            // Reuse the result object for a different lookup
            lookup.Lookup("martygalski".AsSpan(), reuse: result);
            assertEquals(0, result.Count);
            assertEquals("martygalski", result.Word.ToString());
        }

        [Test]
        public void TestIndexerAndOutOfRange()
        {
            string dict = "test-prefix.dict";
            var lookup = new DictionaryLookup(ReadDictionary(dict));
            var result = lookup.Lookup("Rzeczypospolitej".AsSpan());

            // Valid index access
            WordData firstItem = result[0];
            assertNotNull(firstItem);

            // Out of range accesses
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = result[-1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = result[result.Count]; });
        }

        [Test]
        public void TestEnumeratorBehavior()
        {
            string dict = "test-prefix.dict";
            var lookup = new DictionaryLookup(ReadDictionary(dict));
            var result = lookup.Lookup("Rzeczypospolitej".AsSpan());

            int countViaEnumerator = 0;
            using (var enumerator = result.GetEnumerator())
            {
                // Test explicit IEnumerator.Reset() implementation
                ((IEnumerator)enumerator).Reset();

                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    assertNotNull(current);
                    countViaEnumerator++;
                }
            }

            assertEquals(result.Count, countViaEnumerator);

            // Test non-generic IEnumerable GetEnumerator implementations
            int countViaGenericEnumerable = 0;
            foreach (var item in (IEnumerable<WordData>)result)
            {
                countViaGenericEnumerable++;
            }
            assertEquals(result.Count, countViaGenericEnumerable);

            int countViaNonGenericEnumerable = 0;
            foreach (var item in (IEnumerable)result)
            {
                assertNotNull(item);
                countViaNonGenericEnumerable++;
            }
            assertEquals(result.Count, countViaNonGenericEnumerable);
        }

        [Test]
        public void TestIListCollectionContractExceptions()
        {
            string dict = "test-prefix.dict";
            var lookup = new DictionaryLookup(ReadDictionary(dict));
            var result = lookup.Lookup("Rzeczypospolitej".AsSpan());
            IList<WordData> list = result;

            assertTrue(list.IsReadOnly);

            // Unsupported mutation and search methods should throw NotSupportedException
            Assert.Throws<NotSupportedException>(() => list.Add(result[0]));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.Throws<NotSupportedException>(() => list.Insert(0, result[0]));
            Assert.Throws<NotSupportedException>(() => list.Remove(result[0]));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list.IndexOf(result[0]));
            Assert.Throws<NotSupportedException>(() => list.Contains(result[0]));
            Assert.Throws<NotSupportedException>(() => { list[0] = result[0]; });
        }

        [Test]
        public void TestCopyToValidationAndExecution()
        {
            string dict = "test-prefix.dict";
            var lookup = new DictionaryLookup(ReadDictionary(dict));
            var result = lookup.Lookup("Rzeczypospolitej".AsSpan());
            ICollection<WordData> collection = result;

            // Argument validation checks
            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(new WordData[result.Count], -1));
            Assert.Throws<ArgumentException>(() => collection.CopyTo(new WordData[result.Count - 1], 0));

            // Successful copy
            WordData[] destination = new WordData[result.Count];
            collection.CopyTo(destination, 0);
            assertEquals(result.Count, destination.Length);
            for (int i = 0; i < result.Count; i++)
            {
                assertNotNull(destination[i]);
                assertEquals(result[i].Word.ToString(), destination[i].Word.ToString());
            }
        }
    }
}
