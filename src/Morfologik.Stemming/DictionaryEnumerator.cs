using J2N.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Morfologik.Stemming
{
    /// <summary>
    /// An enumerator over <see cref="WordData"/> entries of a <see cref="Dictionary"/>. The stems can be decoded from compressed format or
    /// the compressed form can be preserved.
    /// </summary>
    public sealed class DictionaryEnumerator : IEnumerator<WordData>
    {
        private readonly Encoding decoder;
        private readonly IEnumerator<ReadOnlyMemory<byte>> entriesIter;
        private readonly WordData entry;
        private readonly byte separator;
        private readonly bool decodeStems;
        private readonly WordDataStorage wordDataStorage;
        private readonly ISequenceEncoder sequenceEncoder;

        private WordData? current = null;

        /// <summary>
        /// Initializes a new instance of <see cref="DictionaryEnumerator"/>.
        /// </summary>
        public DictionaryEnumerator(Dictionary dictionary, Encoding decoder, bool decodeStems)
        {
            this.entriesIter = dictionary.FSA.GetEnumerator();
            this.separator = dictionary.Metadata.Separator;
            this.sequenceEncoder = dictionary.Metadata.SequenceEncoderType.Get();
            this.decoder = decoder;
            this.wordDataStorage = new WordDataStorage();
            this.entry = new WordData(wordDataStorage);
            this.decodeStems = decodeStems;
        }

        /// <summary>
        /// Moves to the next <see cref="WordData"/> instance.
        /// </summary>
        /// <returns><c>true</c> if the operation was successful; otherwise, <c>false</c> to indicate the end of the set has been reached.</returns>
        public bool MoveNext()
        {
            if (!entriesIter.MoveNext())
                return false;

            ReadOnlyMemory<byte> entryBuffer = entriesIter.Current;

            /*
             * Entries are typically: inflected<SEP>codedBase<SEP>tag so try to find this split.
             */
            ReadOnlySpan<byte> ba = entryBuffer.Span;
            int bbSize = entryBuffer.Length;

            int sepPos;
            for (sepPos = 0; sepPos < bbSize; sepPos++)
            {
                if (ba[sepPos] == separator)
                    break;
            }

            if (sepPos == bbSize)
            {
                throw new Exception("Invalid dictionary entry format (missing separator).");
            }

            wordDataStorage.Clear();
            wordDataStorage.SetDecoder(decoder);

            ArrayBufferWriter<byte> inflectedBuffer = wordDataStorage.WordByteBuffer;
            Span<byte> inflectedWordBytes = inflectedBuffer.GetSpan(sepPos);
            ba.Slice(0, sepPos).CopyTo(inflectedWordBytes);
            inflectedBuffer.Advance(sepPos);

            ArrayBufferWriter<char> inflectedCharBuffer = wordDataStorage.WordCharBuffer;
            int inflectedCharBufferSize = decoder.GetMaxCharCount(sepPos);
            Span<char> inflectedWordChars = inflectedCharBuffer.GetSpan(inflectedCharBufferSize);
            int inflectedWordCharLength = decoder.GetChars(
                inflectedWordBytes.Slice(0, sepPos),
                inflectedWordChars);
            inflectedCharBuffer.Advance(inflectedWordCharLength);

            int encodedStart = sepPos + 1;

            /*
             * Find the next separator byte's position splitting word form and tag.
             */
#pragma warning disable 612, 618
            Debug.Assert(
                sequenceEncoder.PrefixBytes <= bbSize - encodedStart,
                sequenceEncoder.GetType() + " >? " + (bbSize - encodedStart));

            int stemEnd = encodedStart + sequenceEncoder.PrefixBytes;
#pragma warning restore 612, 618

            for (; stemEnd < bbSize; stemEnd++)
            {
                if (ba[stemEnd] == separator)
                    break;
            }

            int encodedStemLength = stemEnd - encodedStart;

            /*
             * Decode the stem into stem buffer.
             */
            ArrayBufferWriter<byte> stemBuffer = wordDataStorage.StemByteBuffer;

            if (decodeStems)
            {
                int maxDecodedByteCount = sequenceEncoder.GetMaxDecodedByteCount(
                    inflectedBuffer.WrittenCount,
                    encodedStemLength);

                Span<byte> stemDecodedBytes = stemBuffer.GetSpan(maxDecodedByteCount);

                if (!sequenceEncoder.TryDecode(
                    inflectedBuffer.WrittenSpan,
                    ba.Slice(encodedStart, encodedStemLength),
                    stemDecodedBytes,
                    out int stemBytesWritten))
                {
                    throw new InvalidOperationException(
                        "The sequence encoder produced more decoded bytes than its maximum byte count.");
                }

                stemBuffer.Advance(stemBytesWritten);
            }
            else
            {
                ba.Slice(encodedStart, encodedStemLength).CopyTo(
                    stemBuffer.GetSpan(encodedStemLength));

                stemBuffer.Advance(encodedStemLength);
            }

            // Skip separator character, if present.
            int tagStart = stemEnd < bbSize ? stemEnd + 1 : bbSize;
            int tagLength = bbSize - tagStart;

            /*
             * Decode the tag data.
             */
            ArrayBufferWriter<byte> tagBuffer = wordDataStorage.TagByteBuffer;
            ba.Slice(tagStart, tagLength).CopyTo(tagBuffer.GetSpan(tagLength));
            tagBuffer.Advance(tagLength);

            return true;
        }

        /// <summary>
        /// Gets the current <see cref="WordData"/>.
        /// </summary>
        public WordData Current => entry;

        object IEnumerator.Current => Current;

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">Always.</exception>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Disposes all resources associated with the enumerator.
        /// </summary>
        public void Dispose()
        {
            entriesIter.Dispose();
        }

        // Remove() not supported in .NET
    }
}
