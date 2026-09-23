using J2N.Text;
using Morfologik.Fsa;
using Morfologik.Stemming.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Morfologik.Stemming
{
    /// <summary>
    /// This class implements a dictionary lookup of an inflected word over a
    /// dictionary previously compiled using the 
    /// <c>dict_compile</c> tool.
    /// </summary>
    public sealed class DictionaryLookup : IStemmer, IEnumerable<WordData>
    {
        /// <summary>An FSA used for lookups.</summary>
        private readonly FSATraversal matcher;

        /// <summary>An iterator for walking along the final states of <see cref="fsa"/>.</summary>
        private readonly ByteSequenceEnumerator finalStatesIterator;

        /// <summary>FSA's root node.</summary>
        private readonly int rootNode;

        /// <summary>
        /// Features of the compiled dictionary.
        /// </summary>
        /// <seealso cref="DictionaryMetadata"/>
        private readonly DictionaryMetadata dictionaryMetadata;

        /// <summary>
        /// Charset encoder for the FSA.
        /// </summary>
        private readonly Encoding encoder;

        /// <summary>
        /// Charset decoder for the FSA.
        /// </summary>
        private readonly Encoding decoder;

        /// <summary>
        /// The FSA we are using.
        /// </summary>
        private readonly FSA fsa;

        /// <seealso cref="SeparatorChar"/>
        private readonly char separatorChar;

        /// <summary>
        /// The <see cref="Stemming.Dictionary"/> this lookup is using.
        /// </summary>
        private readonly Dictionary dictionary;

        private readonly ISequenceEncoder sequenceEncoder;

        /// <summary>
        /// Creates a new object of this class using the given FSA for word lookups
        /// and encoding for converting characters to bytes.
        /// </summary>
        /// <param name="dictionary">The dictionary to use for lookups.</param>
        /// <exception cref="ArgumentException">If FSA's root node cannot be acquired (dictionary is empty).</exception>
        public DictionaryLookup(Dictionary dictionary)
        {
            this.dictionary = dictionary;
            this.dictionaryMetadata = dictionary.Metadata;
            this.sequenceEncoder = dictionary.Metadata.SequenceEncoderType.Get();
            this.rootNode = dictionary.FSA.GetRootNode();
            this.fsa = dictionary.FSA;
            this.matcher = new FSATraversal(fsa);
            this.finalStatesIterator = new ByteSequenceEnumerator(fsa, fsa.GetRootNode());

            if (dictionaryMetadata == null)
            {
                throw new ArgumentNullException(nameof(dictionaryMetadata),
                    "Dictionary metadata must not be null.");
            }

            decoder = dictionary.Metadata.Decoder;
            encoder = dictionary.Metadata.Encoder;
            separatorChar = dictionary.Metadata.SeparatorAsChar;
        }

        static DictionaryLookup()
        {
            EncodingProviderInitializer.EnsureInitialized(); // Morfologik.Stemming specific - initialize encoding provider
        }

        /// <summary>
        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
        /// followed by a separator. The result is a stem (decompressed accordingly
        /// to the dictionary's specification) and an optional tag data.
        /// </summary>
        /// <param name="word">The word to lookup.</param>
        /// <param name="reuse">A <see cref="DictionaryLookupResult"/> instance to reuse
        /// or <c>null</c> to create a new instance. If not <c>null</c>, this same instance
        /// will be returned.</param>
        /// <returns>A list of <see cref="WordData"/> entries (possibly empty).</returns>
        /// <remarks>
        /// This method is immutable and threadsafe.
        /// </remarks>
        public DictionaryLookupResult Lookup(ReadOnlySpan<char> word, DictionaryLookupResult? reuse = default)
        {
            if (reuse is null)
            {
                reuse = new DictionaryLookupResult();
            }
            else
            {
                reuse.Clear();
            }
            reuse.SetDecoder(decoder);

            if (dictionaryMetadata.InputConversionPairs.Count > 0)
            {
                using PooledTextBuilder sb = new(word, capacity: word.Length + 16);
                ApplyReplacements(sb, dictionaryMetadata.InputConversionPairs);
                return LookupCore(sb.AsSpan(), reuse);
            }

            return LookupCore(word, reuse);
        }

        private DictionaryLookupResult LookupCore(ReadOnlySpan<char> word, DictionaryLookupResult result)
        {
            byte separator = dictionaryMetadata.Separator;
#pragma warning disable 612, 618
            int prefixBytes = sequenceEncoder.PrefixBytes;
#pragma warning restore 612, 618

            if (word.IndexOf(separatorChar) > -1)
            {
                // No valid input can contain the separator.
                result.Clear();
                return result;
            }

            int wordByteBufferLength = encoder.GetMaxByteCount(word.Length);
            Span<byte> wordByteBuffer = result.WordBytesBuffer.GetSpan(wordByteBufferLength);
            try
            {
                // Allow this to throw - we catch it below and return an empty result.
                // This is important because silently replacing characters would incorrectly
                // change the lookup to something else.
                int wordByteLength = encoder.GetBytes(word, wordByteBuffer);

                // Try to find a partial match in the dictionary.
                MatchResult match = matcher.Match(wordByteBuffer.Slice(0, wordByteLength), rootNode);

                if (match.Kind == MatchResultKind.SequenceIsAPrefix)
                {
                    /*
                     * The entire sequence exists in the dictionary. A separator should
                     * be the next symbol.
                     */
                    int arc = fsa.GetArc(match.Node, separator);

                    /*
                     * The situation when the arc points to a final node should NEVER
                     * happen. After all, we want the word to have SOME base form.
                     */
                    if (arc != 0 && !fsa.IsArcFinal(arc))
                    {
                        // There is such a word in the dictionary. Return its base forms.
                        //int formsCount = 0;

                        if (dictionaryMetadata.OutputConversionPairs.Count == 0)
                        {
                            result.SetWord(word);
                        }
                        else
                        {
                            using PooledTextBuilder outputWord = new(word, capacity: word.Length + 16);
                            ApplyReplacements(outputWord, dictionaryMetadata.OutputConversionPairs);
                            result.SetWord(outputWord.AsSpan());
                        }

                        finalStatesIterator.RestartFrom(fsa.GetEndNode(arc));
                        while (finalStatesIterator.MoveNext())
                        {
                            ReadOnlyMemory<byte> bb = finalStatesIterator.Current;
                            ReadOnlySpan<byte> ba = bb.Span;
                            int bbSize = ba.Length;

                            /*
                            * Find the separator byte's position splitting the inflection instructions
                            * from the tag.
                            */
                            Debug.Assert(prefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
                            int sepPos;
                            for (sepPos = prefixBytes; sepPos < bbSize; sepPos++)
                            {
                                if (ba[sepPos] == separator)
                                {
                                    break;
                                }
                            }

                            /*
                            * Decode the stem into the stem buffer.
                            */
                            int encodedLength = sepPos;
                            int stemByteBufferCount = sequenceEncoder.GetMaxDecodedByteCount(wordByteLength, encodedLength);
                            int stemByteOffset = result.StemBytesBuffer.WrittenCount;
                            Span<byte> stemDecodedBuffer = result.StemBytesBuffer.GetSpan(stemByteBufferCount);

                            bool success = sequenceEncoder.TryDecode(wordByteBuffer.Slice(0, wordByteLength), ba.Slice(0, encodedLength), stemDecodedBuffer, out int stemByteCount);
                            if (!success)
                            {
                                throw new InvalidOperationException("The stem sequence decoder produced more decoded bytes than its maximum byte count.");
                            }
                            result.StemBytesBuffer.Advance(stemByteCount);

                            // Skip separator character.
                            sepPos++;

                            /*
                            * Decode the tag data.
                            */
                            int tagSize = bbSize - sepPos;
                            int tagByteOffset = result.TagBytesBuffer.WrittenCount;
                            int tagByteCount = tagSize;

                            if (tagSize > 0)
                            {
                                Span<byte> tagByteDestination = result.TagBytesBuffer.GetSpan(tagSize);
                                ba.Slice(sepPos, tagSize).CopyTo(tagByteDestination);
                                result.TagBytesBuffer.Advance(tagSize);
                            }

                            result.AddEntry(
                                stemByteOffset,
                                stemByteCount,
                                tagByteOffset,
                                tagByteCount);
                        }
                    }
                }
                return result;
            }
            catch (EncoderFallbackException)
            {
                // This should be a rare occurrence, but if it happens it means there is no way
                // the dictionary can contain the input word.
                result.Clear();
                return result;
            }
        }

        /// <summary>
        /// Apply partial string replacements from a given dictionary.
        /// <para/>
        /// Useful if the word needs to be normalized somehow (i.e., ligatures,
        /// apostrophes and such).
        /// </summary>
        /// <param name="word">The word to apply replacements to.</param>
        /// <param name="replacements">A dictionary of replacements (from-&gt;to).</param>
        /// <returns>New string with all replacements applied.</returns>
        public static string ApplyReplacements(ReadOnlySpan<char> word, IDictionary<string, string> replacements) // Morfologik.Stemming TODO: Ideally, the string would be written to a Span<char> on the public API - need to reassess
        {
            // quite horrible from performance point of view; this should really be a transducer.
            using PooledTextBuilder sb = new(word, capacity: word.Length + 16);
            ApplyReplacements(sb, replacements);
            return sb.ToString();
        }

        /// <summary>
        /// Apply partial string replacements from a given dictionary.
        /// <para/>
        /// Useful if the word needs to be normalized somehow (i.e., ligatures,
        /// apostrophes and such).
        /// </summary>
        /// <param name="word">The word to apply replacements to.</param>
        /// <param name="replacements">A dictionary of replacements (from-&gt;to).</param>
        /// <returns>New string with all replacements applied.</returns>
        private static void ApplyReplacements(PooledTextBuilder word, IDictionary<string, string> replacements)
        {
            foreach (var e in replacements)
            {
                word.Replace(e.Key, e.Value);
            }
        }

        /// <summary>
        /// Return an enumerator over all <see cref="WordData"/> entries available in the
        /// embedded <see cref="Stemming.Dictionary"/>
        /// </summary>
        public IEnumerator<WordData> GetEnumerator()
        {
            return new DictionaryEnumerator(dictionary, decoder, true);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Return the <see cref="Stemming.Dictionary"/> used by this object.
        /// </summary>
        public Dictionary Dictionary => dictionary;

        /// <summary>
        /// Returns the logical separator character splitting inflected form,
        /// lemma correction token and a tag. Note that this character is a best-effort
        /// conversion from a byte in <see cref="DictionaryMetadata.Separator"/> and
        /// may not be valid in the target encoding (although this is highly unlikely).
        /// </summary>
        public char SeparatorChar => separatorChar;
    }
}
