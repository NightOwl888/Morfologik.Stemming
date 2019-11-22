using J2N.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Morfologik.Stemming.Polish
{
    /// <summary>
    /// A dictionary-based stemmer for the Polish language. Instances of this class
    /// are not thread safe.
    /// </summary>
    /// <seealso cref="Morfologik.Stemming.DictionaryLookup"/>
    public sealed class PolishStemmer : IStemmer, IEnumerable<WordData>
    {
        private const string ResourcePath = "Morfologik.Stemming.Polish.";
        private const string DictionaryName = "polish.dict";

        /// <summary>The underlying dictionary, loaded once (lazily).</summary>
        private static readonly Dictionary dictionary = LoadDictionary();

        /// <summary>Dictionary lookup delegate.</summary>
        private readonly DictionaryLookup lookup;

        private static Dictionary LoadDictionary()
        {
            Type type = typeof(PolishStemmer);
            lock (type)
            {
                string dict = ResourcePath + DictionaryName;
                using (var dictStream = type.GetTypeInfo().Assembly.GetManifestResourceStream(dict))
                using (var metadataStream = type.GetTypeInfo().Assembly.GetManifestResourceStream(DictionaryMetadata.GetExpectedMetadataFileName(dict)))
                {
                    if (dictStream == null)
                    {
                        throw new IOException("Polish dictionary resource not found.");
                    }

                    return Dictionary.Read(dictStream, metadataStream);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PolishStemmer"/>.
        /// </summary>
        public PolishStemmer()
        {
            lookup = new DictionaryLookup(dictionary);
        }

        /// <summary>
        /// Gets the underlying <see cref="Morfologik.Stemming.Dictionary"/> driving the stemmer.
        /// </summary>
        public Dictionary Dictionary => dictionary;

        /// <summary>
        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
        /// followed by a separator. The result is a stem (decompressed accordingly
        /// to the dictionary's specification) and an optional tag data.
        /// </summary>
        public IList<WordData> Lookup(ICharSequence word) => lookup.Lookup(word);

        /// <summary>
        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
        /// followed by a separator. The result is a stem (decompressed accordingly
        /// to the dictionary's specification) and an optional tag data.
        /// </summary>
        public IList<WordData> Lookup(char[] word) => lookup.Lookup(word);

        /// <summary>
        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
        /// followed by a separator. The result is a stem (decompressed accordingly
        /// to the dictionary's specification) and an optional tag data.
        /// </summary>
        public IList<WordData> Lookup(StringBuilder word) => lookup.Lookup(word);

        /// <summary>
        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
        /// followed by a separator. The result is a stem (decompressed accordingly
        /// to the dictionary's specification) and an optional tag data.
        /// </summary>
        public IList<WordData> Lookup(string word) => lookup.Lookup(word);


        /// <summary>
        /// Iterates over all dictionary forms stored in this stemmer.
        /// </summary>
        public IEnumerator<WordData> GetEnumerator()
        {
            return lookup.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
