using J2N.Text;
using Morfologik.Fsa;
using Morfologik.Stemming.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private const int CharStackBufferSize = 64;

        /// <summary>An FSA used for lookups.</summary>
        private readonly FSATraversal matcher;

        /// <summary>An iterator for walking along the final states of <see cref="fsa"/>.</summary>
        private readonly ByteSequenceEnumerator finalStatesIterator;

        /// <summary>FSA's root node.</summary>
        private readonly int rootNode;

        ///// <summary>Expand buffers and arrays by this constant.</summary>
        //private const int ExpandSize = 10;

        ///// <summary>Private internal array of reusable word data objects.</summary>
        //private WordData[] forms = new WordData[0];

        ///// <summary>A "view" over an array implementing</summary>
        //private readonly ArrayViewList<WordData> formsList;

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

        ///// <summary>
        ///// Internal reusable buffer for encoding words into byte arrays using
        ///// <see cref="encoder"/>.
        ///// </summary>
        //private ByteBuffer byteBuffer = ByteBuffer.Allocate(0);

        ///// <summary>
        ///// Internal reusable buffer for encoding words into byte arrays using
        ///// <see cref="encoder"/>.
        ///// </summary>
        //private CharBuffer charBuffer = CharBuffer.Allocate(0);

        /// <summary>
        /// Reusable match result.
        /// </summary>
        private readonly MatchResult matchResult = new MatchResult();

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
            //this.formsList = new ArrayViewList<WordData>(forms, 0, forms.Length);

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




////        /// <summary>
////        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
////        /// followed by a separator. The result is a stem (decompressed accordingly
////        /// to the dictionary's specification) and an optional tag data.
////        /// </summary>
////        public IList<WordData> Lookup(ICharSequence word)
////        {
////            byte separator = dictionaryMetadata.Separator;
////#pragma warning disable 612, 618
////            int prefixBytes = sequenceEncoder.PrefixBytes;
////#pragma warning restore 612, 618

////            if (dictionaryMetadata.InputConversionPairs.Count > 0)
////            {
////                word = ApplyReplacements(word, dictionaryMetadata.InputConversionPairs);
////            }

////            // Reset the output list to zero length.
////            formsList.Wrap(forms, 0, 0);

////            // Encode word characters into bytes in the same encoding as the FSA's.
////            charBuffer = BufferUtils.ClearAndEnsureCapacity(charBuffer, word.Length);
////            for (int i = 0; i < word.Length; i++)
////            {
////                char chr = word[i];
////                if (chr == separatorChar)
////                {
////                    // No valid input can contain the separator.
////                    return formsList;
////                }
////                charBuffer.Put(chr);
////            }
////            charBuffer.Flip();
////            try
////            {
////                byteBuffer = BufferUtils.CharsToBytes(encoder, charBuffer, byteBuffer);
////            }
////            catch (UnmappableInputException)
////            {
////                // This should be a rare occurrence, but if it happens it means there is no way
////                // the dictionary can contain the input word.
////                return formsList;
////            }

////            // Try to find a partial match in the dictionary.
////            MatchResult match = matcher.Match(matchResult, byteBuffer
////                .Array, 0, byteBuffer.Remaining, rootNode);

////            if (match.Kind == MatchResult.SequenceIsAPrefix)
////            {
////                /*
////                 * The entire sequence exists in the dictionary. A separator should
////                 * be the next symbol.
////                 */
////                int arc = fsa.GetArc(match.Node, separator);

////                /*
////                 * The situation when the arc points to a final node should NEVER
////                 * happen. After all, we want the word to have SOME base form.
////                 */
////                if (arc != 0 && !fsa.IsArcFinal(arc))
////                {
////                    // There is such a word in the dictionary. Return its base forms.
////                    int formsCount = 0;

////                    finalStatesIterator.RestartFrom(fsa.GetEndNode(arc));
////                    while (finalStatesIterator.MoveNext())
////                    {
////                        ByteBuffer bb = finalStatesIterator.Current;
////                        byte[] ba = bb.Array;
////                        int bbSize = bb.Remaining;

////                        if (formsCount >= forms.Length)
////                        {
////                            //forms = Arrays.CopyOf(forms, forms.Length + EXPAND_SIZE);
////                            Array.Resize(ref forms, forms.Length + ExpandSize);
////                            for (int k = 0; k < forms.Length; k++)
////                            {
////                                if (forms[k] == null)
////                                    forms[k] = new WordData(decoder);
////                            }
////                        }

////                        /*
////                         * Now, expand the prefix/ suffix 'compression' and store
////                         * the base form.
////                         */
////                        WordData wordData = forms[formsCount++];
////                        if (!dictionaryMetadata.OutputConversionPairs.Any())
////                        {
////                            wordData.Update(byteBuffer, word);
////                        }
////                        else
////                        {
////                            wordData.Update(byteBuffer, ApplyReplacements(word, dictionaryMetadata.OutputConversionPairs));
////                        }

////                        /*
////                         * Find the separator byte's position splitting the inflection instructions
////                         * from the tag.
////                         */
////                        Debug.Assert(prefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
////                        int sepPos;
////                        for (sepPos = prefixBytes; sepPos < bbSize; sepPos++)
////                        {
////                            if (ba[sepPos] == separator)
////                            {
////                                break;
////                            }
////                        }

////                        /////*
////                        //// * Decode the stem into stem buffer.
////                        //// */
////                        ////wordData.stemBuffer = sequenceEncoder.Decode(wordData.stemBuffer,
////                        ////                                         byteBuffer,
////                        ////                                         ByteBuffer.Wrap(ba, 0, sepPos));

////                        /*
////                         * Decode the stem into the stem buffer.
////                         */
////                        int encodedLength = sepPos;
////                        int maxDecodedByteCount =
////                            sequenceEncoder.GetMaxDecodedByteCount(
////                                byteBuffer.Remaining,
////                                encodedLength);

////                        wordData.stemBuffer =
////                            BufferUtils.ClearAndEnsureCapacity(
////                                wordData.stemBuffer,
////                                maxDecodedByteCount);

////                        if (!sequenceEncoder.TryDecode(
////                            byteBuffer.Array.AsSpan(
////                                byteBuffer.Position,
////                                byteBuffer.Remaining),
////                            ba.AsSpan(0, encodedLength),
////                            wordData.stemBuffer.Array.AsSpan(0, maxDecodedByteCount),
////                            out int bytesWritten))
////                        {
////                            // The destination was sized using GetMaxDecodedByteCount(),
////                            // so this should never happen for a valid encoder.
////                            throw new InvalidOperationException(
////                                "The sequence encoder produced more decoded bytes than its maximum byte count.");
////                        }

////                        wordData.stemBuffer.Limit = bytesWritten;
////                        wordData.stemBuffer.Position = 0;

////                        // Skip separator character.
////                        sepPos++;

////                        /*
////                         * Decode the tag data.
////                         */
////                        int tagSize = bbSize - sepPos;
////                        if (tagSize > 0)
////                        {
////                            wordData.tagBuffer = BufferUtils.ClearAndEnsureCapacity(wordData.tagBuffer, tagSize);
////                            wordData.tagBuffer.Put(ba, sepPos, tagSize);
////                            wordData.tagBuffer.Flip();
////                        }
////                    }

////                    formsList.Wrap(forms, 0, formsCount);
////                }
////            }
////            else
////            {
////                /*
////                 * this case is somewhat confusing: we should have hit the separator
////                 * first... I don't really know how to deal with it at the time
////                 * being.
////                 */
////            }
////            return formsList;
////        }

////        /// <summary>
////        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
////        /// followed by a separator. The result is a stem (decompressed accordingly
////        /// to the dictionary's specification) and an optional tag data.
////        /// </summary>
////        public IList<WordData> Lookup(char[] word)
////        {
////            byte separator = dictionaryMetadata.Separator;
////#pragma warning disable 612, 618
////            int prefixBytes = sequenceEncoder.PrefixBytes;
////#pragma warning restore 612, 618

////            if (dictionaryMetadata.InputConversionPairs.Any())
////            {
////                word = ApplyReplacements(word, dictionaryMetadata.InputConversionPairs);
////            }

////            // Reset the output list to zero length.
////            formsList.Wrap(forms, 0, 0);

////            // Encode word characters into bytes in the same encoding as the FSA's.
////            charBuffer = BufferUtils.ClearAndEnsureCapacity(charBuffer, word.Length);
////            for (int i = 0; i < word.Length; i++)
////            {
////                char chr = word[i];
////                if (chr == separatorChar)
////                {
////                    // No valid input can contain the separator.
////                    return formsList;
////                }
////                charBuffer.Put(chr);
////            }
////            charBuffer.Flip();
////            try
////            {
////                byteBuffer = BufferUtils.CharsToBytes(encoder, charBuffer, byteBuffer);
////            }
////            catch (UnmappableInputException)
////            {
////                // This should be a rare occurrence, but if it happens it means there is no way
////                // the dictionary can contain the input word.
////                return formsList;
////            }

////            // Try to find a partial match in the dictionary.
////            MatchResult match = matcher.Match(matchResult, byteBuffer
////                .Array, 0, byteBuffer.Remaining, rootNode);

////            if (match.Kind == MatchResult.SequenceIsAPrefix)
////            {
////                /*
////                 * The entire sequence exists in the dictionary. A separator should
////                 * be the next symbol.
////                 */
////                int arc = fsa.GetArc(match.Node, separator);

////                /*
////                 * The situation when the arc points to a final node should NEVER
////                 * happen. After all, we want the word to have SOME base form.
////                 */
////                if (arc != 0 && !fsa.IsArcFinal(arc))
////                {
////                    // There is such a word in the dictionary. Return its base forms.
////                    int formsCount = 0;

////                    finalStatesIterator.RestartFrom(fsa.GetEndNode(arc));
////                    while (finalStatesIterator.MoveNext())
////                    {
////                        ByteBuffer bb = finalStatesIterator.Current;
////                        byte[] ba = bb.Array;
////                        int bbSize = bb.Remaining;

////                        if (formsCount >= forms.Length)
////                        {
////                            //forms = Arrays.CopyOf(forms, forms.Length + EXPAND_SIZE);
////                            Array.Resize(ref forms, forms.Length + ExpandSize);
////                            for (int k = 0; k < forms.Length; k++)
////                            {
////                                if (forms[k] == null)
////                                    forms[k] = new WordData(decoder);
////                            }
////                        }

////                        /*
////                         * Now, expand the prefix/ suffix 'compression' and store
////                         * the base form.
////                         */
////                        WordData wordData = forms[formsCount++];
////                        if (!dictionaryMetadata.OutputConversionPairs.Any())
////                        {
////                            wordData.Update(byteBuffer, word);
////                        }
////                        else
////                        {
////                            wordData.Update(byteBuffer, ApplyReplacements(word, dictionaryMetadata.OutputConversionPairs));
////                        }

////                        /*
////                         * Find the separator byte's position splitting the inflection instructions
////                         * from the tag.
////                         */
////                        Debug.Assert(prefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
////                        int sepPos;
////                        for (sepPos = prefixBytes; sepPos < bbSize; sepPos++)
////                        {
////                            if (ba[sepPos] == separator)
////                            {
////                                break;
////                            }
////                        }

////                        /////*
////                        //// * Decode the stem into stem buffer.
////                        //// */
////                        ////wordData.stemBuffer = sequenceEncoder.Decode(wordData.stemBuffer,
////                        ////                                         byteBuffer,
////                        ////                                         ByteBuffer.Wrap(ba, 0, sepPos));

////                        /*
////                         * Decode the stem into the stem buffer.
////                         */
////                        int encodedLength = sepPos;
////                        int maxDecodedByteCount =
////                            sequenceEncoder.GetMaxDecodedByteCount(
////                                byteBuffer.Remaining,
////                                encodedLength);

////                        wordData.stemBuffer =
////                            BufferUtils.ClearAndEnsureCapacity(
////                                wordData.stemBuffer,
////                                maxDecodedByteCount);

////                        if (!sequenceEncoder.TryDecode(
////                            byteBuffer.Array.AsSpan(
////                                byteBuffer.Position,
////                                byteBuffer.Remaining),
////                            ba.AsSpan(0, encodedLength),
////                            wordData.stemBuffer.Array.AsSpan(0, maxDecodedByteCount),
////                            out int bytesWritten))
////                        {
////                            // The destination was sized using GetMaxDecodedByteCount(),
////                            // so this should never happen for a valid encoder.
////                            throw new InvalidOperationException(
////                                "The sequence encoder produced more decoded bytes than its maximum byte count.");
////                        }

////                        wordData.stemBuffer.Limit = bytesWritten;
////                        wordData.stemBuffer.Position = 0;

////                        // Skip separator character.
////                        sepPos++;

////                        /*
////                         * Decode the tag data.
////                         */
////                        int tagSize = bbSize - sepPos;
////                        if (tagSize > 0)
////                        {
////                            wordData.tagBuffer = BufferUtils.ClearAndEnsureCapacity(wordData.tagBuffer, tagSize);
////                            wordData.tagBuffer.Put(ba, sepPos, tagSize);
////                            wordData.tagBuffer.Flip();
////                        }
////                    }

////                    formsList.Wrap(forms, 0, formsCount);
////                }
////            }
////            else
////            {
////                /*
////                 * this case is somewhat confusing: we should have hit the separator
////                 * first... I don't really know how to deal with it at the time
////                 * being.
////                 */
////            }
////            return formsList;
////        }

////        /// <summary>
////        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
////        /// followed by a separator. The result is a stem (decompressed accordingly
////        /// to the dictionary's specification) and an optional tag data.
////        /// </summary>
////        public IList<WordData> Lookup(StringBuilder word)
////        {
////            byte separator = dictionaryMetadata.Separator;
////#pragma warning disable 612, 618
////            int prefixBytes = sequenceEncoder.PrefixBytes;
////#pragma warning restore 612, 618

////            if (dictionaryMetadata.InputConversionPairs.Any())
////            {
////                word = ApplyReplacements(word, dictionaryMetadata.InputConversionPairs);
////            }

////            // Reset the output list to zero length.
////            formsList.Wrap(forms, 0, 0);

////            // Encode word characters into bytes in the same encoding as the FSA's.
////            charBuffer = BufferUtils.ClearAndEnsureCapacity(charBuffer, word.Length);
////            for (int i = 0; i < word.Length; i++)
////            {
////                char chr = word[i];
////                if (chr == separatorChar)
////                {
////                    // No valid input can contain the separator.
////                    return formsList;
////                }
////                charBuffer.Put(chr);
////            }
////            charBuffer.Flip();
////            try
////            {
////                byteBuffer = BufferUtils.CharsToBytes(encoder, charBuffer, byteBuffer);
////            }
////            catch (UnmappableInputException)
////            {
////                // This should be a rare occurrence, but if it happens it means there is no way
////                // the dictionary can contain the input word.
////                return formsList;
////            }

////            // Try to find a partial match in the dictionary.
////            MatchResult match = matcher.Match(matchResult, byteBuffer
////                .Array, 0, byteBuffer.Remaining, rootNode);

////            if (match.Kind == MatchResult.SequenceIsAPrefix)
////            {
////                /*
////                 * The entire sequence exists in the dictionary. A separator should
////                 * be the next symbol.
////                 */
////                int arc = fsa.GetArc(match.Node, separator);

////                /*
////                 * The situation when the arc points to a final node should NEVER
////                 * happen. After all, we want the word to have SOME base form.
////                 */
////                if (arc != 0 && !fsa.IsArcFinal(arc))
////                {
////                    // There is such a word in the dictionary. Return its base forms.
////                    int formsCount = 0;

////                    finalStatesIterator.RestartFrom(fsa.GetEndNode(arc));
////                    while (finalStatesIterator.MoveNext())
////                    {
////                        ByteBuffer bb = finalStatesIterator.Current;
////                        byte[] ba = bb.Array;
////                        int bbSize = bb.Remaining;

////                        if (formsCount >= forms.Length)
////                        {
////                            //forms = Arrays.CopyOf(forms, forms.Length + EXPAND_SIZE);
////                            Array.Resize(ref forms, forms.Length + ExpandSize);
////                            for (int k = 0; k < forms.Length; k++)
////                            {
////                                if (forms[k] == null)
////                                    forms[k] = new WordData(decoder);
////                            }
////                        }

////                        /*
////                         * Now, expand the prefix/ suffix 'compression' and store
////                         * the base form.
////                         */
////                        WordData wordData = forms[formsCount++];
////                        if (!dictionaryMetadata.OutputConversionPairs.Any())
////                        {
////                            wordData.Update(byteBuffer, word);
////                        }
////                        else
////                        {
////                            wordData.Update(byteBuffer, ApplyReplacements(word, dictionaryMetadata.OutputConversionPairs));
////                        }

////                        /*
////                         * Find the separator byte's position splitting the inflection instructions
////                         * from the tag.
////                         */
////                        Debug.Assert(prefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
////                        int sepPos;
////                        for (sepPos = prefixBytes; sepPos < bbSize; sepPos++)
////                        {
////                            if (ba[sepPos] == separator)
////                            {
////                                break;
////                            }
////                        }

////                        /////*
////                        //// * Decode the stem into stem buffer.
////                        //// */
////                        ////wordData.stemBuffer = sequenceEncoder.Decode(wordData.stemBuffer,
////                        ////                                         byteBuffer,
////                        ////                                         ByteBuffer.Wrap(ba, 0, sepPos));


////                        /*
////                         * Decode the stem into the stem buffer.
////                         */
////                        int encodedLength = sepPos;
////                        int maxDecodedByteCount =
////                            sequenceEncoder.GetMaxDecodedByteCount(
////                                byteBuffer.Remaining,
////                                encodedLength);

////                        wordData.stemBuffer =
////                            BufferUtils.ClearAndEnsureCapacity(
////                                wordData.stemBuffer,
////                                maxDecodedByteCount);

////                        if (!sequenceEncoder.TryDecode(
////                            byteBuffer.Array.AsSpan(
////                                byteBuffer.Position,
////                                byteBuffer.Remaining),
////                            ba.AsSpan(0, encodedLength),
////                            wordData.stemBuffer.Array.AsSpan(0, maxDecodedByteCount),
////                            out int bytesWritten))
////                        {
////                            // The destination was sized using GetMaxDecodedByteCount(),
////                            // so this should never happen for a valid encoder.
////                            throw new InvalidOperationException(
////                                "The sequence encoder produced more decoded bytes than its maximum byte count.");
////                        }

////                        wordData.stemBuffer.Limit = bytesWritten;
////                        wordData.stemBuffer.Position = 0;

////                        // Skip separator character.
////                        sepPos++;

////                        /*
////                         * Decode the tag data.
////                         */
////                        int tagSize = bbSize - sepPos;
////                        if (tagSize > 0)
////                        {
////                            wordData.tagBuffer = BufferUtils.ClearAndEnsureCapacity(wordData.tagBuffer, tagSize);
////                            wordData.tagBuffer.Put(ba, sepPos, tagSize);
////                            wordData.tagBuffer.Flip();
////                        }
////                    }

////                    formsList.Wrap(forms, 0, formsCount);
////                }
////            }
////            else
////            {
////                /*
////                 * this case is somewhat confusing: we should have hit the separator
////                 * first... I don't really know how to deal with it at the time
////                 * being.
////                 */
////            }
////            return formsList;
////        }

////        /// <summary>
////        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
////        /// followed by a separator. The result is a stem (decompressed accordingly
////        /// to the dictionary's specification) and an optional tag data.
////        /// </summary>
////        public IList<WordData> Lookup(string word)
////        {
////            byte separator = dictionaryMetadata.Separator;
////#pragma warning disable 612, 618
////            int prefixBytes = sequenceEncoder.PrefixBytes;
////#pragma warning restore 612, 618

////            if (dictionaryMetadata.InputConversionPairs.Any())
////            {
////                word = ApplyReplacements(word, dictionaryMetadata.InputConversionPairs);
////            }

////            // Reset the output list to zero length.
////            formsList.Wrap(forms, 0, 0);

////            // Encode word characters into bytes in the same encoding as the FSA's.
////            charBuffer = BufferUtils.ClearAndEnsureCapacity(charBuffer, word.Length);
////            for (int i = 0; i < word.Length; i++)
////            {
////                char chr = word[i];
////                if (chr == separatorChar)
////                {
////                    // No valid input can contain the separator.
////                    return formsList;
////                }
////                charBuffer.Put(chr);
////            }
////            charBuffer.Flip();
////            try
////            {
////                byteBuffer = BufferUtils.CharsToBytes(encoder, charBuffer, byteBuffer);
////            }
////            catch (UnmappableInputException)
////            {
////                // This should be a rare occurrence, but if it happens it means there is no way
////                // the dictionary can contain the input word.
////                return formsList;
////            }

////            // Try to find a partial match in the dictionary.
////            MatchResult match = matcher.Match(matchResult, byteBuffer
////                .Array, 0, byteBuffer.Remaining, rootNode);

////            if (match.Kind == MatchResult.SequenceIsAPrefix)
////            {
////                /*
////                 * The entire sequence exists in the dictionary. A separator should
////                 * be the next symbol.
////                 */
////                int arc = fsa.GetArc(match.Node, separator);

////                /*
////                 * The situation when the arc points to a final node should NEVER
////                 * happen. After all, we want the word to have SOME base form.
////                 */
////                if (arc != 0 && !fsa.IsArcFinal(arc))
////                {
////                    // There is such a word in the dictionary. Return its base forms.
////                    int formsCount = 0;

////                    finalStatesIterator.RestartFrom(fsa.GetEndNode(arc));
////                    while (finalStatesIterator.MoveNext())
////                    {
////                        ByteBuffer bb = finalStatesIterator.Current;
////                        byte[] ba = bb.Array;
////                        int bbSize = bb.Remaining;

////                        if (formsCount >= forms.Length)
////                        {
////                            //forms = Arrays.CopyOf(forms, forms.Length + EXPAND_SIZE);
////                            Array.Resize(ref forms, forms.Length + ExpandSize);
////                            for (int k = 0; k < forms.Length; k++)
////                            {
////                                if (forms[k] == null)
////                                    forms[k] = new WordData(decoder);
////                            }
////                        }

////                        /*
////                         * Now, expand the prefix/ suffix 'compression' and store
////                         * the base form.
////                         */
////                        WordData wordData = forms[formsCount++];
////                        if (!dictionaryMetadata.OutputConversionPairs.Any())
////                        {
////                            wordData.Update(byteBuffer, word);
////                        }
////                        else
////                        {
////                            wordData.Update(byteBuffer, ApplyReplacements(word, dictionaryMetadata.OutputConversionPairs));
////                        }

////                        /*
////                         * Find the separator byte's position splitting the inflection instructions
////                         * from the tag.
////                         */
////                        Debug.Assert(prefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
////                        int sepPos;
////                        for (sepPos = prefixBytes; sepPos < bbSize; sepPos++)
////                        {
////                            if (ba[sepPos] == separator)
////                            {
////                                break;
////                            }
////                        }

////                        /////*
////                        //// * Decode the stem into stem buffer.
////                        //// */
////                        ////wordData.stemBuffer = sequenceEncoder.Decode(wordData.stemBuffer,
////                        ////                                         byteBuffer,
////                        ////                                         ByteBuffer.Wrap(ba, 0, sepPos));


////                        /*
////                         * Decode the stem into the stem buffer.
////                         */
////                        int encodedLength = sepPos;
////                        int maxDecodedByteCount =
////                            sequenceEncoder.GetMaxDecodedByteCount(
////                                byteBuffer.Remaining,
////                                encodedLength);

////                        wordData.stemBuffer =
////                            BufferUtils.ClearAndEnsureCapacity(
////                                wordData.stemBuffer,
////                                maxDecodedByteCount);

////                        if (!sequenceEncoder.TryDecode(
////                            byteBuffer.Array.AsSpan(
////                                byteBuffer.Position,
////                                byteBuffer.Remaining),
////                            ba.AsSpan(0, encodedLength),
////                            wordData.stemBuffer.Array.AsSpan(0, maxDecodedByteCount),
////                            out int bytesWritten))
////                        {
////                            // The destination was sized using GetMaxDecodedByteCount(),
////                            // so this should never happen for a valid encoder.
////                            throw new InvalidOperationException(
////                                "The sequence encoder produced more decoded bytes than its maximum byte count.");
////                        }

////                        wordData.stemBuffer.Limit = bytesWritten;
////                        wordData.stemBuffer.Position = 0;


////                        // Skip separator character.
////                        sepPos++;

////                        /*
////                         * Decode the tag data.
////                         */
////                        int tagSize = bbSize - sepPos;
////                        if (tagSize > 0)
////                        {
////                            wordData.tagBuffer = BufferUtils.ClearAndEnsureCapacity(wordData.tagBuffer, tagSize);
////                            wordData.tagBuffer.Put(ba, sepPos, tagSize);
////                            wordData.tagBuffer.Flip();
////                        }
////                    }

////                    formsList.Wrap(forms, 0, formsCount);
////                }
////            }
////            else
////            {
////                /*
////                 * this case is somewhat confusing: we should have hit the separator
////                 * first... I don't really know how to deal with it at the time
////                 * being.
////                 */
////            }
////            return formsList;
////        }

        ///// <summary>
        ///// Apply partial string replacements from a given dictionary.
        ///// <para/>
        ///// Useful if the word needs to be normalized somehow (i.e., ligatures,
        ///// apostrophes and such).
        ///// </summary>
        ///// <param name="word">The word to apply replacements to.</param>
        ///// <param name="replacements">A dictionary of replacements (from-&gt;to).</param>
        ///// <returns>New string with all replacements applied.</returns>
        //public static ICharSequence ApplyReplacements(ICharSequence word, IDictionary<string, string> replacements)
        //{
        //    // quite horrible from performance point of view; this should really be a transducer.
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(charSequence: word);
        //    foreach (var e in replacements)
        //    {
        //        sb.Replace(e.Key, e.Value);
        //    }
        //    return sb.AsCharSequence();
        //}

        ///// <summary>
        ///// Apply partial string replacements from a given dictionary.
        ///// <para/>
        ///// Useful if the word needs to be normalized somehow (i.e., ligatures,
        ///// apostrophes and such).
        ///// </summary>
        ///// <param name="word">The word to apply replacements to.</param>
        ///// <param name="replacements">A dictionary of replacements (from-&gt;to).</param>
        ///// <returns>New string with all replacements applied.</returns>
        //public static char[] ApplyReplacements(char[] word, IDictionary<string, string> replacements)
        //{
        //    // quite horrible from performance point of view; this should really be a transducer.
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(word);
        //    foreach (var e in replacements)
        //    {
        //        sb.Replace(e.Key, e.Value);
        //    }
        //    return sb.ToString().ToCharArray();
        //}

        ///// <summary>
        ///// Apply partial string replacements from a given dictionary.
        ///// <para/>
        ///// Useful if the word needs to be normalized somehow (i.e., ligatures,
        ///// apostrophes and such).
        ///// </summary>
        ///// <param name="word">The word to apply replacements to.</param>
        ///// <param name="replacements">A dictionary of replacements (from-&gt;to).</param>
        ///// <returns>New string with all replacements applied.</returns>
        //public static StringBuilder ApplyReplacements(StringBuilder word, IDictionary<string, string> replacements)
        //{
        //    // quite horrible from performance point of view; this should really be a transducer.
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(word);
        //    foreach (var e in replacements)
        //    {
        //        sb.Replace(e.Key, e.Value);
        //    }
        //    return sb;
        //}

        ///// <summary>
        ///// Apply partial string replacements from a given dictionary.
        ///// <para/>
        ///// Useful if the word needs to be normalized somehow (i.e., ligatures,
        ///// apostrophes and such).
        ///// </summary>
        ///// <param name="word">The word to apply replacements to.</param>
        ///// <param name="replacements">A dictionary of replacements (from-&gt;to).</param>
        ///// <returns>New string with all replacements applied.</returns>
        //public static string ApplyReplacements(string word, IDictionary<string, string> replacements)
        //{
        //    // quite horrible from performance point of view; this should really be a transducer.
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(word);
        //    foreach (var e in replacements)
        //    {
        //        sb.Replace(e.Key, e.Value);
        //    }
        //    return sb.ToString();
        //}

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
                ValueStringBuilder sb = word.Length <= CharStackBufferSize
                    ? new(stackalloc char[CharStackBufferSize])
                    : new(word.Length);
                try
                {
                    sb.Append(word);
                    ApplyReplacements(ref sb, dictionaryMetadata.InputConversionPairs);

                    return LookupCore(sb.AsSpan(), reuse);
                }
                finally
                {
                    sb.Dispose();
                }
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
                MatchResult match = matcher.Match(matchResult, wordByteBuffer.Slice(0, wordByteLength), rootNode);

                if (match.Kind == MatchResult.SequenceIsAPrefix)
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
                            ValueStringBuilder outputWord = word.Length <= CharStackBufferSize
                                ? new(stackalloc char[CharStackBufferSize])
                                : new(word.Length);
                            try
                            {
                                outputWord.Append(word);
                                ApplyReplacements(ref outputWord, dictionaryMetadata.OutputConversionPairs);
                                result.SetWord(outputWord.AsSpan());
                            }
                            finally
                            {
                                outputWord.Dispose();
                            }
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


        //        private DictionaryLookupResult LookupCore(ReadOnlySpan<char> word, DictionaryLookupResult result)
        //        {
        //            result.Clear();
        //            byte separator = dictionaryMetadata.Separator;
        //#pragma warning disable 612, 618
        //            int prefixBytes = sequenceEncoder.PrefixBytes;
        //#pragma warning restore 612, 618

        //            if (word.IndexOf(separatorChar) > -1)
        //            {
        //                // No valid input can contain the separator.
        //                return result;
        //            }

        //            int byteBufferLength = encoder.GetMaxByteCount(word.Length);
        //            byte[]? arrayToReturnToPool = null;
        //            Span<byte> byteBuffer = byteBufferLength <= ByteStackBufferSize
        //                ? stackalloc byte[byteBufferLength]
        //                : (arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(byteBufferLength));
        //            try
        //            {
        //                if (!encoder.TryGetBytes(word, byteBuffer, out int byteLength))
        //                {
        //                    throw new InvalidOperationException("The encoder produced more bytes than its maximum byte count.");
        //                }

        //                // Try to find a partial match in the dictionary.
        //                MatchResult match = matcher.Match(matchResult, byteBuffer.Slice(0, byteLength), rootNode);

        //                if (match.Kind == MatchResult.SequenceIsAPrefix)
        //                {
        //                    /*
        //                     * The entire sequence exists in the dictionary. A separator should
        //                     * be the next symbol.
        //                     */
        //                    int arc = fsa.GetArc(match.Node, separator);

        //                    /*
        //                     * The situation when the arc points to a final node should NEVER
        //                     * happen. After all, we want the word to have SOME base form.
        //                     */
        //                    if (arc != 0 && !fsa.IsArcFinal(arc))
        //                    {
        //                        // There is such a word in the dictionary. Return its base forms.
        //                        //int formsCount = 0;

        //                        if (dictionaryMetadata.OutputConversionPairs.Count == 0)
        //                        {
        //                            result.SetWord(word);
        //                        }
        //                        else
        //                        {
        //                            ValueStringBuilder outputWord = new(stackalloc char[256]);
        //                            try
        //                            {
        //                                outputWord.Append(word);
        //                                ApplyReplacements(ref outputWord, dictionaryMetadata.OutputConversionPairs);
        //                                result.SetWord(outputWord.AsSpan());
        //                            }
        //                            finally
        //                            {
        //                                outputWord.Dispose();
        //                            }
        //                        }

        //                        finalStatesIterator.RestartFrom(fsa.GetEndNode(arc));
        //                        while (finalStatesIterator.MoveNext())
        //                        {
        //                            ByteBuffer bb = finalStatesIterator.Current;
        //                            byte[] ba = bb.Array;
        //                            int bbSize = bb.Remaining;

        //                            /*
        //                            * Find the separator byte's position splitting the inflection instructions
        //                            * from the tag.
        //                            */
        //                            Debug.Assert(prefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
        //                            int sepPos;
        //                            for (sepPos = prefixBytes; sepPos < bbSize; sepPos++)
        //                            {
        //                                if (ba[sepPos] == separator)
        //                                {
        //                                    break;
        //                                }
        //                            }

        //                            /*
        //                            * Decode the stem into the stem buffer.
        //                            */
        //                            int encodedLength = sepPos;
        //                            int maxDecodedByteCount = sequenceEncoder.GetMaxDecodedByteCount(byteLength, encodedLength);

        //                            byte[]? decodedArrayToReturnToPool = null;
        //                            //Span<byte> decodedBuffer = maxDecodedByteCount <= ByteStackBufferSize
        //                            //    ? stackalloc byte[maxDecodedByteCount]
        //                            //    : (decodedArrayToReturnToPool = ArrayPool<byte>.Shared.Rent(maxDecodedByteCount));
        //                            Span<byte> decodedBuffer = (decodedArrayToReturnToPool = ArrayPool<byte>.Shared.Rent(maxDecodedByteCount));
        //                            try
        //                            {
        //                                bool success = sequenceEncoder.TryDecode(byteBuffer.Slice(0, byteLength), ba.AsSpan(0, encodedLength), decodedBuffer, out int decodedByteLength);
        //                                Debug.Assert(success, "The stem sequence decoder produced more decoded bytes than its maximum byte count.");
        //                                if (!success)
        //                                {
        //                                    throw new InvalidOperationException("The stem sequence decoder produced more decoded bytes than its maximum byte count.");
        //                                }

        //                                int maxStemCharCount = decoder.GetMaxCharCount(decodedByteLength);
        //                                int stemOffset = result.StemCharsBuffer.WrittenCount;

        //                                Span<char> stemDestination = result.StemCharsBuffer.GetSpan(maxStemCharCount);
        //                                int stemCharCount = decoder.GetChars(decodedBuffer.Slice(0, decodedByteLength), stemDestination);

        //                                result.StemCharsBuffer.Advance(stemCharCount);

        //                                // Skip separator character.
        //                                sepPos++;

        //                                /*
        //                                * Decode the tag data.
        //                                */
        //                                int tagSize = bbSize - sepPos;
        //                                int tagOffset = result.TagCharsBuffer.WrittenCount;
        //                                int tagCharCount = 0;

        //                                if (tagSize > 0)
        //                                {
        //                                    int maxTagCharCount = decoder.GetMaxCharCount(tagSize);

        //                                    Span<char> tagDestination = result.TagCharsBuffer.GetSpan(maxTagCharCount);
        //                                    tagCharCount = decoder.GetChars(ba.AsSpan(sepPos, tagSize), tagDestination);

        //                                    result.TagCharsBuffer.Advance(tagCharCount);
        //                                }

        //                                result.AddEntry(
        //                                    stemOffset,
        //                                    stemCharCount,
        //                                    tagOffset,
        //                                    tagCharCount);
        //                            }
        //                            finally
        //                            {
        //                                if (decodedArrayToReturnToPool is not null)
        //                                    ArrayPool<byte>.Shared.Return(decodedArrayToReturnToPool, clearArray: true);
        //                            }
        //                        }
        //                    }
        //                }

        //                return result;
        //            }
        //            catch (EncoderFallbackException)
        //            {
        //                // This should be a rare occurrence, but if it happens it means there is no way
        //                // the dictionary can contain the input word.
        //                result.Clear();
        //                return result;
        //            }
        //            finally
        //            {
        //                if (arrayToReturnToPool is not null)
        //                    ArrayPool<byte>.Shared.Return(arrayToReturnToPool, clearArray: true);
        //            }
        //        }

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
            ValueStringBuilder sb = new ValueStringBuilder(stackalloc char[256]);
            try
            {
                sb.Append(word);
                ApplyReplacements(ref sb, replacements);
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }

        internal static void ApplyReplacements(ref ValueStringBuilder word, IDictionary<string, string> replacements)
        {
            foreach (var e in replacements)
            {
                string key = e.Key;
                int index = word.IndexOf(key);
                while (index != -1)
                {
                    word.Replace(index, key.Length, e.Value);
                    index = word.IndexOf(key, index + e.Value.Length);
                }
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




    //    /// <summary>
    //    /// This class implements a dictionary lookup of an inflected word over a
    //    /// dictionary previously compiled using the 
    //    /// <c>dict_compile</c> tool.
    //    /// </summary>
    //    public sealed class DictionaryLookup : IStemmer, IEnumerable<WordData>
    //    {
    //        /// <summary>An FSA used for lookups.</summary>
    //        private readonly FSATraversal matcher;

    //        /// <summary>An iterator for walking along the final states of <see cref="fsa"/>.</summary>
    //        private readonly ByteSequenceEnumerator finalStatesIterator;

    //        /// <summary>FSA's root node.</summary>
    //        private readonly int rootNode;

    //        /// <summary>Expand buffers and arrays by this constant.</summary>
    //        private const int ExpandSize = 10;

    //        /// <summary>Private internal array of reusable word data objects.</summary>
    //        private WordData[] forms = new WordData[0];

    //        /// <summary>A "view" over an array implementing</summary>
    //        private readonly ArrayViewList<WordData> formsList;

    //        /// <summary>
    //        /// Features of the compiled dictionary.
    //        /// </summary>
    //        /// <seealso cref="DictionaryMetadata"/>
    //        private readonly DictionaryMetadata dictionaryMetadata;

    //        /// <summary>
    //        /// Charset encoder for the FSA.
    //        /// </summary>
    //        private readonly Encoding encoder;

    //        /// <summary>
    //        /// Charset decoder for the FSA.
    //        /// </summary>
    //        private readonly Encoding decoder;

    //        /// <summary>
    //        /// The FSA we are using.
    //        /// </summary>
    //        private readonly FSA fsa;

    //        /// <seealso cref="SeparatorChar"/>
    //        private readonly char separatorChar;

    //        /// <summary>
    //        /// Internal reusable buffer for encoding words into byte arrays using
    //        /// <see cref="encoder"/>.
    //        /// </summary>
    //        private ByteBuffer byteBuffer = ByteBuffer.Allocate(0);

    //        /// <summary>
    //        /// Internal reusable buffer for encoding words into byte arrays using
    //        /// <see cref="encoder"/>.
    //        /// </summary>
    //        private CharBuffer charBuffer = CharBuffer.Allocate(0);

    //        /// <summary>
    //        /// Reusable match result.
    //        /// </summary>
    //        private readonly MatchResult matchResult = new MatchResult();

    //        /// <summary>
    //        /// The <see cref="Stemming.Dictionary"/> this lookup is using.
    //        /// </summary>
    //        private readonly Dictionary dictionary;

    //        private readonly ISequenceEncoder sequenceEncoder;

    //        /// <summary>
    //        /// Creates a new object of this class using the given FSA for word lookups
    //        /// and encoding for converting characters to bytes.
    //        /// </summary>
    //        /// <param name="dictionary">The dictionary to use for lookups.</param>
    //        /// <exception cref="ArgumentException">If FSA's root node cannot be acquired (dictionary is empty).</exception>
    //        public DictionaryLookup(Dictionary dictionary)
    //        {
    //            this.formsList = new ArrayViewList<WordData>(forms, 0, forms.Length);

    //            this.dictionary = dictionary;
    //            this.dictionaryMetadata = dictionary.Metadata;
    //            this.sequenceEncoder = dictionary.Metadata.SequenceEncoderType.Get();
    //            this.rootNode = dictionary.FSA.GetRootNode();
    //            this.fsa = dictionary.FSA;
    //            this.matcher = new FSATraversal(fsa);
    //            this.finalStatesIterator = new ByteSequenceEnumerator(fsa, fsa.GetRootNode());

    //            if (dictionaryMetadata == null)
    //            {
    //                throw new ArgumentNullException(nameof(dictionaryMetadata),
    //                    "Dictionary metadata must not be null.");
    //            }

    //            decoder = dictionary.Metadata.Decoder;
    //            encoder = dictionary.Metadata.Encoder;
    //            separatorChar = dictionary.Metadata.SeparatorAsChar;
    //        }

    //        static DictionaryLookup()
    //        {
    //            EncodingProviderInitializer.EnsureInitialized(); // Morfologik.Stemming specific - initialize encoding provider
    //        }

    //        /// <summary>
    //        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
    //        /// followed by a separator. The result is a stem (decompressed accordingly
    //        /// to the dictionary's specification) and an optional tag data.
    //        /// </summary>
    //        public IList<WordData> Lookup(ICharSequence word)
    //        {
    //            byte separator = dictionaryMetadata.Separator;
    //#pragma warning disable 612, 618
    //            int prefixBytes = sequenceEncoder.PrefixBytes;
    //#pragma warning restore 612, 618

    //            if (dictionaryMetadata.InputConversionPairs.Count > 0)
    //            {
    //                word = ApplyReplacements(word, dictionaryMetadata.InputConversionPairs);
    //            }

    //            // Reset the output list to zero length.
    //            formsList.Wrap(forms, 0, 0);

    //            // Encode word characters into bytes in the same encoding as the FSA's.
    //            charBuffer = BufferUtils.ClearAndEnsureCapacity(charBuffer, word.Length);
    //            for (int i = 0; i < word.Length; i++)
    //            {
    //                char chr = word[i];
    //                if (chr == separatorChar)
    //                {
    //                    // No valid input can contain the separator.
    //                    return formsList;
    //                }
    //                charBuffer.Put(chr);
    //            }
    //            charBuffer.Flip();
    //            try
    //            {
    //                byteBuffer = BufferUtils.CharsToBytes(encoder, charBuffer, byteBuffer);
    //            }
    //            catch (UnmappableInputException)
    //            {
    //                // This should be a rare occurrence, but if it happens it means there is no way
    //                // the dictionary can contain the input word.
    //                return formsList;
    //            }

    //            // Try to find a partial match in the dictionary.
    //            MatchResult match = matcher.Match(matchResult, byteBuffer
    //                .Array, 0, byteBuffer.Remaining, rootNode);

    //            if (match.Kind == MatchResult.SequenceIsAPrefix)
    //            {
    //                /*
    //                 * The entire sequence exists in the dictionary. A separator should
    //                 * be the next symbol.
    //                 */
    //                int arc = fsa.GetArc(match.Node, separator);

    //                /*
    //                 * The situation when the arc points to a final node should NEVER
    //                 * happen. After all, we want the word to have SOME base form.
    //                 */
    //                if (arc != 0 && !fsa.IsArcFinal(arc))
    //                {
    //                    // There is such a word in the dictionary. Return its base forms.
    //                    int formsCount = 0;

    //                    finalStatesIterator.RestartFrom(fsa.GetEndNode(arc));
    //                    while (finalStatesIterator.MoveNext())
    //                    {
    //                        ByteBuffer bb = finalStatesIterator.Current;
    //                        byte[] ba = bb.Array;
    //                        int bbSize = bb.Remaining;

    //                        if (formsCount >= forms.Length)
    //                        {
    //                            //forms = Arrays.CopyOf(forms, forms.Length + EXPAND_SIZE);
    //                            Array.Resize(ref forms, forms.Length + ExpandSize);
    //                            for (int k = 0; k < forms.Length; k++)
    //                            {
    //                                if (forms[k] == null)
    //                                    forms[k] = new WordData(decoder);
    //                            }
    //                        }

    //                        /*
    //                         * Now, expand the prefix/ suffix 'compression' and store
    //                         * the base form.
    //                         */
    //                        WordData wordData = forms[formsCount++];
    //                        if (!dictionaryMetadata.OutputConversionPairs.Any())
    //                        {
    //                            wordData.Update(byteBuffer, word);
    //                        }
    //                        else
    //                        {
    //                            wordData.Update(byteBuffer, ApplyReplacements(word, dictionaryMetadata.OutputConversionPairs));
    //                        }

    //                        /*
    //                         * Find the separator byte's position splitting the inflection instructions
    //                         * from the tag.
    //                         */
    //                        Debug.Assert(prefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
    //                        int sepPos;
    //                        for (sepPos = prefixBytes; sepPos < bbSize; sepPos++)
    //                        {
    //                            if (ba[sepPos] == separator)
    //                            {
    //                                break;
    //                            }
    //                        }

    //                        /////*
    //                        //// * Decode the stem into stem buffer.
    //                        //// */
    //                        ////wordData.stemBuffer = sequenceEncoder.Decode(wordData.stemBuffer,
    //                        ////                                         byteBuffer,
    //                        ////                                         ByteBuffer.Wrap(ba, 0, sepPos));

    //                        /*
    //                         * Decode the stem into the stem buffer.
    //                         */
    //                        int encodedLength = sepPos;
    //                        int maxDecodedByteCount =
    //                            sequenceEncoder.GetMaxDecodedByteCount(
    //                                byteBuffer.Remaining,
    //                                encodedLength);

    //                        wordData.stemBuffer =
    //                            BufferUtils.ClearAndEnsureCapacity(
    //                                wordData.stemBuffer,
    //                                maxDecodedByteCount);

    //                        if (!sequenceEncoder.TryDecode(
    //                            byteBuffer.Array.AsSpan(
    //                                byteBuffer.Position,
    //                                byteBuffer.Remaining),
    //                            ba.AsSpan(0, encodedLength),
    //                            wordData.stemBuffer.Array.AsSpan(0, maxDecodedByteCount),
    //                            out int bytesWritten))
    //                        {
    //                            // The destination was sized using GetMaxDecodedByteCount(),
    //                            // so this should never happen for a valid encoder.
    //                            throw new InvalidOperationException(
    //                                "The sequence encoder produced more decoded bytes than its maximum byte count.");
    //                        }

    //                        wordData.stemBuffer.Limit = bytesWritten;
    //                        wordData.stemBuffer.Position = 0;

    //                        // Skip separator character.
    //                        sepPos++;

    //                        /*
    //                         * Decode the tag data.
    //                         */
    //                        int tagSize = bbSize - sepPos;
    //                        if (tagSize > 0)
    //                        {
    //                            wordData.tagBuffer = BufferUtils.ClearAndEnsureCapacity(wordData.tagBuffer, tagSize);
    //                            wordData.tagBuffer.Put(ba, sepPos, tagSize);
    //                            wordData.tagBuffer.Flip();
    //                        }
    //                    }

    //                    formsList.Wrap(forms, 0, formsCount);
    //                }
    //            }
    //            else
    //            {
    //                /*
    //                 * this case is somewhat confusing: we should have hit the separator
    //                 * first... I don't really know how to deal with it at the time
    //                 * being.
    //                 */
    //            }
    //            return formsList;
    //        }

    //        /// <summary>
    //        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
    //        /// followed by a separator. The result is a stem (decompressed accordingly
    //        /// to the dictionary's specification) and an optional tag data.
    //        /// </summary>
    //        public IList<WordData> Lookup(char[] word)
    //        {
    //            byte separator = dictionaryMetadata.Separator;
    //#pragma warning disable 612, 618
    //            int prefixBytes = sequenceEncoder.PrefixBytes;
    //#pragma warning restore 612, 618

    //            if (dictionaryMetadata.InputConversionPairs.Any())
    //            {
    //                word = ApplyReplacements(word, dictionaryMetadata.InputConversionPairs);
    //            }

    //            // Reset the output list to zero length.
    //            formsList.Wrap(forms, 0, 0);

    //            // Encode word characters into bytes in the same encoding as the FSA's.
    //            charBuffer = BufferUtils.ClearAndEnsureCapacity(charBuffer, word.Length);
    //            for (int i = 0; i < word.Length; i++)
    //            {
    //                char chr = word[i];
    //                if (chr == separatorChar)
    //                {
    //                    // No valid input can contain the separator.
    //                    return formsList;
    //                }
    //                charBuffer.Put(chr);
    //            }
    //            charBuffer.Flip();
    //            try
    //            {
    //                byteBuffer = BufferUtils.CharsToBytes(encoder, charBuffer, byteBuffer);
    //            }
    //            catch (UnmappableInputException)
    //            {
    //                // This should be a rare occurrence, but if it happens it means there is no way
    //                // the dictionary can contain the input word.
    //                return formsList;
    //            }

    //            // Try to find a partial match in the dictionary.
    //            MatchResult match = matcher.Match(matchResult, byteBuffer
    //                .Array, 0, byteBuffer.Remaining, rootNode);

    //            if (match.Kind == MatchResult.SequenceIsAPrefix)
    //            {
    //                /*
    //                 * The entire sequence exists in the dictionary. A separator should
    //                 * be the next symbol.
    //                 */
    //                int arc = fsa.GetArc(match.Node, separator);

    //                /*
    //                 * The situation when the arc points to a final node should NEVER
    //                 * happen. After all, we want the word to have SOME base form.
    //                 */
    //                if (arc != 0 && !fsa.IsArcFinal(arc))
    //                {
    //                    // There is such a word in the dictionary. Return its base forms.
    //                    int formsCount = 0;

    //                    finalStatesIterator.RestartFrom(fsa.GetEndNode(arc));
    //                    while (finalStatesIterator.MoveNext())
    //                    {
    //                        ByteBuffer bb = finalStatesIterator.Current;
    //                        byte[] ba = bb.Array;
    //                        int bbSize = bb.Remaining;

    //                        if (formsCount >= forms.Length)
    //                        {
    //                            //forms = Arrays.CopyOf(forms, forms.Length + EXPAND_SIZE);
    //                            Array.Resize(ref forms, forms.Length + ExpandSize);
    //                            for (int k = 0; k < forms.Length; k++)
    //                            {
    //                                if (forms[k] == null)
    //                                    forms[k] = new WordData(decoder);
    //                            }
    //                        }

    //                        /*
    //                         * Now, expand the prefix/ suffix 'compression' and store
    //                         * the base form.
    //                         */
    //                        WordData wordData = forms[formsCount++];
    //                        if (!dictionaryMetadata.OutputConversionPairs.Any())
    //                        {
    //                            wordData.Update(byteBuffer, word);
    //                        }
    //                        else
    //                        {
    //                            wordData.Update(byteBuffer, ApplyReplacements(word, dictionaryMetadata.OutputConversionPairs));
    //                        }

    //                        /*
    //                         * Find the separator byte's position splitting the inflection instructions
    //                         * from the tag.
    //                         */
    //                        Debug.Assert(prefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
    //                        int sepPos;
    //                        for (sepPos = prefixBytes; sepPos < bbSize; sepPos++)
    //                        {
    //                            if (ba[sepPos] == separator)
    //                            {
    //                                break;
    //                            }
    //                        }

    //                        /////*
    //                        //// * Decode the stem into stem buffer.
    //                        //// */
    //                        ////wordData.stemBuffer = sequenceEncoder.Decode(wordData.stemBuffer,
    //                        ////                                         byteBuffer,
    //                        ////                                         ByteBuffer.Wrap(ba, 0, sepPos));

    //                        /*
    //                         * Decode the stem into the stem buffer.
    //                         */
    //                        int encodedLength = sepPos;
    //                        int maxDecodedByteCount =
    //                            sequenceEncoder.GetMaxDecodedByteCount(
    //                                byteBuffer.Remaining,
    //                                encodedLength);

    //                        wordData.stemBuffer =
    //                            BufferUtils.ClearAndEnsureCapacity(
    //                                wordData.stemBuffer,
    //                                maxDecodedByteCount);

    //                        if (!sequenceEncoder.TryDecode(
    //                            byteBuffer.Array.AsSpan(
    //                                byteBuffer.Position,
    //                                byteBuffer.Remaining),
    //                            ba.AsSpan(0, encodedLength),
    //                            wordData.stemBuffer.Array.AsSpan(0, maxDecodedByteCount),
    //                            out int bytesWritten))
    //                        {
    //                            // The destination was sized using GetMaxDecodedByteCount(),
    //                            // so this should never happen for a valid encoder.
    //                            throw new InvalidOperationException(
    //                                "The sequence encoder produced more decoded bytes than its maximum byte count.");
    //                        }

    //                        wordData.stemBuffer.Limit = bytesWritten;
    //                        wordData.stemBuffer.Position = 0;

    //                        // Skip separator character.
    //                        sepPos++;

    //                        /*
    //                         * Decode the tag data.
    //                         */
    //                        int tagSize = bbSize - sepPos;
    //                        if (tagSize > 0)
    //                        {
    //                            wordData.tagBuffer = BufferUtils.ClearAndEnsureCapacity(wordData.tagBuffer, tagSize);
    //                            wordData.tagBuffer.Put(ba, sepPos, tagSize);
    //                            wordData.tagBuffer.Flip();
    //                        }
    //                    }

    //                    formsList.Wrap(forms, 0, formsCount);
    //                }
    //            }
    //            else
    //            {
    //                /*
    //                 * this case is somewhat confusing: we should have hit the separator
    //                 * first... I don't really know how to deal with it at the time
    //                 * being.
    //                 */
    //            }
    //            return formsList;
    //        }

    //        /// <summary>
    //        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
    //        /// followed by a separator. The result is a stem (decompressed accordingly
    //        /// to the dictionary's specification) and an optional tag data.
    //        /// </summary>
    //        public IList<WordData> Lookup(StringBuilder word)
    //        {
    //            byte separator = dictionaryMetadata.Separator;
    //#pragma warning disable 612, 618
    //            int prefixBytes = sequenceEncoder.PrefixBytes;
    //#pragma warning restore 612, 618

    //            if (dictionaryMetadata.InputConversionPairs.Any())
    //            {
    //                word = ApplyReplacements(word, dictionaryMetadata.InputConversionPairs);
    //            }

    //            // Reset the output list to zero length.
    //            formsList.Wrap(forms, 0, 0);

    //            // Encode word characters into bytes in the same encoding as the FSA's.
    //            charBuffer = BufferUtils.ClearAndEnsureCapacity(charBuffer, word.Length);
    //            for (int i = 0; i < word.Length; i++)
    //            {
    //                char chr = word[i];
    //                if (chr == separatorChar)
    //                {
    //                    // No valid input can contain the separator.
    //                    return formsList;
    //                }
    //                charBuffer.Put(chr);
    //            }
    //            charBuffer.Flip();
    //            try
    //            {
    //                byteBuffer = BufferUtils.CharsToBytes(encoder, charBuffer, byteBuffer);
    //            }
    //            catch (UnmappableInputException)
    //            {
    //                // This should be a rare occurrence, but if it happens it means there is no way
    //                // the dictionary can contain the input word.
    //                return formsList;
    //            }

    //            // Try to find a partial match in the dictionary.
    //            MatchResult match = matcher.Match(matchResult, byteBuffer
    //                .Array, 0, byteBuffer.Remaining, rootNode);

    //            if (match.Kind == MatchResult.SequenceIsAPrefix)
    //            {
    //                /*
    //                 * The entire sequence exists in the dictionary. A separator should
    //                 * be the next symbol.
    //                 */
    //                int arc = fsa.GetArc(match.Node, separator);

    //                /*
    //                 * The situation when the arc points to a final node should NEVER
    //                 * happen. After all, we want the word to have SOME base form.
    //                 */
    //                if (arc != 0 && !fsa.IsArcFinal(arc))
    //                {
    //                    // There is such a word in the dictionary. Return its base forms.
    //                    int formsCount = 0;

    //                    finalStatesIterator.RestartFrom(fsa.GetEndNode(arc));
    //                    while (finalStatesIterator.MoveNext())
    //                    {
    //                        ByteBuffer bb = finalStatesIterator.Current;
    //                        byte[] ba = bb.Array;
    //                        int bbSize = bb.Remaining;

    //                        if (formsCount >= forms.Length)
    //                        {
    //                            //forms = Arrays.CopyOf(forms, forms.Length + EXPAND_SIZE);
    //                            Array.Resize(ref forms, forms.Length + ExpandSize);
    //                            for (int k = 0; k < forms.Length; k++)
    //                            {
    //                                if (forms[k] == null)
    //                                    forms[k] = new WordData(decoder);
    //                            }
    //                        }

    //                        /*
    //                         * Now, expand the prefix/ suffix 'compression' and store
    //                         * the base form.
    //                         */
    //                        WordData wordData = forms[formsCount++];
    //                        if (!dictionaryMetadata.OutputConversionPairs.Any())
    //                        {
    //                            wordData.Update(byteBuffer, word);
    //                        }
    //                        else
    //                        {
    //                            wordData.Update(byteBuffer, ApplyReplacements(word, dictionaryMetadata.OutputConversionPairs));
    //                        }

    //                        /*
    //                         * Find the separator byte's position splitting the inflection instructions
    //                         * from the tag.
    //                         */
    //                        Debug.Assert(prefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
    //                        int sepPos;
    //                        for (sepPos = prefixBytes; sepPos < bbSize; sepPos++)
    //                        {
    //                            if (ba[sepPos] == separator)
    //                            {
    //                                break;
    //                            }
    //                        }

    //                        /////*
    //                        //// * Decode the stem into stem buffer.
    //                        //// */
    //                        ////wordData.stemBuffer = sequenceEncoder.Decode(wordData.stemBuffer,
    //                        ////                                         byteBuffer,
    //                        ////                                         ByteBuffer.Wrap(ba, 0, sepPos));


    //                        /*
    //                         * Decode the stem into the stem buffer.
    //                         */
    //                        int encodedLength = sepPos;
    //                        int maxDecodedByteCount =
    //                            sequenceEncoder.GetMaxDecodedByteCount(
    //                                byteBuffer.Remaining,
    //                                encodedLength);

    //                        wordData.stemBuffer =
    //                            BufferUtils.ClearAndEnsureCapacity(
    //                                wordData.stemBuffer,
    //                                maxDecodedByteCount);

    //                        if (!sequenceEncoder.TryDecode(
    //                            byteBuffer.Array.AsSpan(
    //                                byteBuffer.Position,
    //                                byteBuffer.Remaining),
    //                            ba.AsSpan(0, encodedLength),
    //                            wordData.stemBuffer.Array.AsSpan(0, maxDecodedByteCount),
    //                            out int bytesWritten))
    //                        {
    //                            // The destination was sized using GetMaxDecodedByteCount(),
    //                            // so this should never happen for a valid encoder.
    //                            throw new InvalidOperationException(
    //                                "The sequence encoder produced more decoded bytes than its maximum byte count.");
    //                        }

    //                        wordData.stemBuffer.Limit = bytesWritten;
    //                        wordData.stemBuffer.Position = 0;

    //                        // Skip separator character.
    //                        sepPos++;

    //                        /*
    //                         * Decode the tag data.
    //                         */
    //                        int tagSize = bbSize - sepPos;
    //                        if (tagSize > 0)
    //                        {
    //                            wordData.tagBuffer = BufferUtils.ClearAndEnsureCapacity(wordData.tagBuffer, tagSize);
    //                            wordData.tagBuffer.Put(ba, sepPos, tagSize);
    //                            wordData.tagBuffer.Flip();
    //                        }
    //                    }

    //                    formsList.Wrap(forms, 0, formsCount);
    //                }
    //            }
    //            else
    //            {
    //                /*
    //                 * this case is somewhat confusing: we should have hit the separator
    //                 * first... I don't really know how to deal with it at the time
    //                 * being.
    //                 */
    //            }
    //            return formsList;
    //        }

    //        /// <summary>
    //        /// Searches the automaton for a symbol sequence equal to <paramref name="word"/>,
    //        /// followed by a separator. The result is a stem (decompressed accordingly
    //        /// to the dictionary's specification) and an optional tag data.
    //        /// </summary>
    //        public IList<WordData> Lookup(string word)
    //        {
    //            byte separator = dictionaryMetadata.Separator;
    //#pragma warning disable 612, 618
    //            int prefixBytes = sequenceEncoder.PrefixBytes;
    //#pragma warning restore 612, 618

    //            if (dictionaryMetadata.InputConversionPairs.Any())
    //            {
    //                word = ApplyReplacements(word, dictionaryMetadata.InputConversionPairs);
    //            }

    //            // Reset the output list to zero length.
    //            formsList.Wrap(forms, 0, 0);

    //            // Encode word characters into bytes in the same encoding as the FSA's.
    //            charBuffer = BufferUtils.ClearAndEnsureCapacity(charBuffer, word.Length);
    //            for (int i = 0; i < word.Length; i++)
    //            {
    //                char chr = word[i];
    //                if (chr == separatorChar)
    //                {
    //                    // No valid input can contain the separator.
    //                    return formsList;
    //                }
    //                charBuffer.Put(chr);
    //            }
    //            charBuffer.Flip();
    //            try
    //            {
    //                byteBuffer = BufferUtils.CharsToBytes(encoder, charBuffer, byteBuffer);
    //            }
    //            catch (UnmappableInputException)
    //            {
    //                // This should be a rare occurrence, but if it happens it means there is no way
    //                // the dictionary can contain the input word.
    //                return formsList;
    //            }

    //            // Try to find a partial match in the dictionary.
    //            MatchResult match = matcher.Match(matchResult, byteBuffer
    //                .Array, 0, byteBuffer.Remaining, rootNode);

    //            if (match.Kind == MatchResult.SequenceIsAPrefix)
    //            {
    //                /*
    //                 * The entire sequence exists in the dictionary. A separator should
    //                 * be the next symbol.
    //                 */
    //                int arc = fsa.GetArc(match.Node, separator);

    //                /*
    //                 * The situation when the arc points to a final node should NEVER
    //                 * happen. After all, we want the word to have SOME base form.
    //                 */
    //                if (arc != 0 && !fsa.IsArcFinal(arc))
    //                {
    //                    // There is such a word in the dictionary. Return its base forms.
    //                    int formsCount = 0;

    //                    finalStatesIterator.RestartFrom(fsa.GetEndNode(arc));
    //                    while (finalStatesIterator.MoveNext())
    //                    {
    //                        ByteBuffer bb = finalStatesIterator.Current;
    //                        byte[] ba = bb.Array;
    //                        int bbSize = bb.Remaining;

    //                        if (formsCount >= forms.Length)
    //                        {
    //                            //forms = Arrays.CopyOf(forms, forms.Length + EXPAND_SIZE);
    //                            Array.Resize(ref forms, forms.Length + ExpandSize);
    //                            for (int k = 0; k < forms.Length; k++)
    //                            {
    //                                if (forms[k] == null)
    //                                    forms[k] = new WordData(decoder);
    //                            }
    //                        }

    //                        /*
    //                         * Now, expand the prefix/ suffix 'compression' and store
    //                         * the base form.
    //                         */
    //                        WordData wordData = forms[formsCount++];
    //                        if (!dictionaryMetadata.OutputConversionPairs.Any())
    //                        {
    //                            wordData.Update(byteBuffer, word);
    //                        }
    //                        else
    //                        {
    //                            wordData.Update(byteBuffer, ApplyReplacements(word, dictionaryMetadata.OutputConversionPairs));
    //                        }

    //                        /*
    //                         * Find the separator byte's position splitting the inflection instructions
    //                         * from the tag.
    //                         */
    //                        Debug.Assert(prefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
    //                        int sepPos;
    //                        for (sepPos = prefixBytes; sepPos < bbSize; sepPos++)
    //                        {
    //                            if (ba[sepPos] == separator)
    //                            {
    //                                break;
    //                            }
    //                        }

    //                        /////*
    //                        //// * Decode the stem into stem buffer.
    //                        //// */
    //                        ////wordData.stemBuffer = sequenceEncoder.Decode(wordData.stemBuffer,
    //                        ////                                         byteBuffer,
    //                        ////                                         ByteBuffer.Wrap(ba, 0, sepPos));


    //                        /*
    //                         * Decode the stem into the stem buffer.
    //                         */
    //                        int encodedLength = sepPos;
    //                        int maxDecodedByteCount =
    //                            sequenceEncoder.GetMaxDecodedByteCount(
    //                                byteBuffer.Remaining,
    //                                encodedLength);

    //                        wordData.stemBuffer =
    //                            BufferUtils.ClearAndEnsureCapacity(
    //                                wordData.stemBuffer,
    //                                maxDecodedByteCount);

    //                        if (!sequenceEncoder.TryDecode(
    //                            byteBuffer.Array.AsSpan(
    //                                byteBuffer.Position,
    //                                byteBuffer.Remaining),
    //                            ba.AsSpan(0, encodedLength),
    //                            wordData.stemBuffer.Array.AsSpan(0, maxDecodedByteCount),
    //                            out int bytesWritten))
    //                        {
    //                            // The destination was sized using GetMaxDecodedByteCount(),
    //                            // so this should never happen for a valid encoder.
    //                            throw new InvalidOperationException(
    //                                "The sequence encoder produced more decoded bytes than its maximum byte count.");
    //                        }

    //                        wordData.stemBuffer.Limit = bytesWritten;
    //                        wordData.stemBuffer.Position = 0;


    //                        // Skip separator character.
    //                        sepPos++;

    //                        /*
    //                         * Decode the tag data.
    //                         */
    //                        int tagSize = bbSize - sepPos;
    //                        if (tagSize > 0)
    //                        {
    //                            wordData.tagBuffer = BufferUtils.ClearAndEnsureCapacity(wordData.tagBuffer, tagSize);
    //                            wordData.tagBuffer.Put(ba, sepPos, tagSize);
    //                            wordData.tagBuffer.Flip();
    //                        }
    //                    }

    //                    formsList.Wrap(forms, 0, formsCount);
    //                }
    //            }
    //            else
    //            {
    //                /*
    //                 * this case is somewhat confusing: we should have hit the separator
    //                 * first... I don't really know how to deal with it at the time
    //                 * being.
    //                 */
    //            }
    //            return formsList;
    //        }

    //        /// <summary>
    //        /// Apply partial string replacements from a given dictionary.
    //        /// <para/>
    //        /// Useful if the word needs to be normalized somehow (i.e., ligatures,
    //        /// apostrophes and such).
    //        /// </summary>
    //        /// <param name="word">The word to apply replacements to.</param>
    //        /// <param name="replacements">A dictionary of replacements (from-&gt;to).</param>
    //        /// <returns>New string with all replacements applied.</returns>
    //        public static ICharSequence ApplyReplacements(ICharSequence word, IDictionary<string, string> replacements)
    //        {
    //            // quite horrible from performance point of view; this should really be a transducer.
    //            StringBuilder sb = new StringBuilder();
    //            sb.Append(charSequence: word);
    //            foreach (var e in replacements)
    //            {
    //                sb.Replace(e.Key, e.Value);
    //            }
    //            return sb.AsCharSequence();
    //        }

    //        /// <summary>
    //        /// Apply partial string replacements from a given dictionary.
    //        /// <para/>
    //        /// Useful if the word needs to be normalized somehow (i.e., ligatures,
    //        /// apostrophes and such).
    //        /// </summary>
    //        /// <param name="word">The word to apply replacements to.</param>
    //        /// <param name="replacements">A dictionary of replacements (from-&gt;to).</param>
    //        /// <returns>New string with all replacements applied.</returns>
    //        public static char[] ApplyReplacements(char[] word, IDictionary<string, string> replacements)
    //        {
    //            // quite horrible from performance point of view; this should really be a transducer.
    //            StringBuilder sb = new StringBuilder();
    //            sb.Append(word);
    //            foreach (var e in replacements)
    //            {
    //                sb.Replace(e.Key, e.Value);
    //            }
    //            return sb.ToString().ToCharArray();
    //        }

    //        /// <summary>
    //        /// Apply partial string replacements from a given dictionary.
    //        /// <para/>
    //        /// Useful if the word needs to be normalized somehow (i.e., ligatures,
    //        /// apostrophes and such).
    //        /// </summary>
    //        /// <param name="word">The word to apply replacements to.</param>
    //        /// <param name="replacements">A dictionary of replacements (from-&gt;to).</param>
    //        /// <returns>New string with all replacements applied.</returns>
    //        public static StringBuilder ApplyReplacements(StringBuilder word, IDictionary<string, string> replacements)
    //        {
    //            // quite horrible from performance point of view; this should really be a transducer.
    //            StringBuilder sb = new StringBuilder();
    //            sb.Append(word);
    //            foreach (var e in replacements)
    //            {
    //                sb.Replace(e.Key, e.Value);
    //            }
    //            return sb;
    //        }

    //        /// <summary>
    //        /// Apply partial string replacements from a given dictionary.
    //        /// <para/>
    //        /// Useful if the word needs to be normalized somehow (i.e., ligatures,
    //        /// apostrophes and such).
    //        /// </summary>
    //        /// <param name="word">The word to apply replacements to.</param>
    //        /// <param name="replacements">A dictionary of replacements (from-&gt;to).</param>
    //        /// <returns>New string with all replacements applied.</returns>
    //        public static string ApplyReplacements(string word, IDictionary<string, string> replacements)
    //        {
    //            // quite horrible from performance point of view; this should really be a transducer.
    //            StringBuilder sb = new StringBuilder();
    //            sb.Append(word);
    //            foreach (var e in replacements)
    //            {
    //                sb.Replace(e.Key, e.Value);
    //            }
    //            return sb.ToString();
    //        }

    //        /// <summary>
    //        /// Return an enumerator over all <see cref="WordData"/> entries available in the
    //        /// embedded <see cref="Stemming.Dictionary"/>
    //        /// </summary>
    //        public IEnumerator<WordData> GetEnumerator()
    //        {
    //            return new DictionaryEnumerator(dictionary, decoder, true);
    //        }

    //        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    //        /// <summary>
    //        /// Return the <see cref="Stemming.Dictionary"/> used by this object.
    //        /// </summary>
    //        public Dictionary Dictionary => dictionary;

    //        /// <summary>
    //        /// Returns the logical separator character splitting inflected form,
    //        /// lemma correction token and a tag. Note that this character is a best-effort
    //        /// conversion from a byte in <see cref="DictionaryMetadata.Separator"/> and
    //        /// may not be valid in the target encoding (although this is highly unlikely).
    //        /// </summary>
    //        public char SeparatorChar => separatorChar;
    //    }
}
