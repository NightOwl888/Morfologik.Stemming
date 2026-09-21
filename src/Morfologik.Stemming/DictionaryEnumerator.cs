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

        //private ByteBuffer inflectedBuffer = ByteBuffer.Allocate(0);
        //private CharBuffer inflectedCharBuffer = CharBuffer.Allocate(0);
        //private ByteBuffer temp = ByteBuffer.Allocate(0);

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
            var hasNext = entriesIter.MoveNext();
            if (!hasNext)
                return false;
            current = Next();
            return current != null;
        }

        /// <summary>
        /// Gets the current <see cref="WordData"/>.
        /// </summary>
        public WordData Current => entry;

        object IEnumerator.Current => Current;

        private WordData Next()
        {
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
                {
                    break;
                }
            }

            if (sepPos == bbSize)
            {
                throw new Exception("Invalid dictionary " + "entry format (missing separator).");
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
            int inflectedWordCharLength = decoder.GetChars(inflectedWordBytes.Slice(0, sepPos), inflectedWordChars);
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

            ArrayBufferWriter<byte> stemBuffer = wordDataStorage.StemByteBuffer;
            ArrayBufferWriter<byte> tagBuffer = wordDataStorage.TagByteBuffer;

            if (decodeStems)
            {
                int encodedStemLength = stemEnd - encodedStart;
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
                int encodedStemLength = stemEnd - encodedStart;
                ba.Slice(encodedStart, encodedStemLength).CopyTo(
                    stemBuffer.GetSpan(encodedStemLength));

                stemBuffer.Advance(encodedStemLength);
            }

            /*
             * Decode the tag data.
             */
            int tagStart = stemEnd < bbSize ? stemEnd + 1 : bbSize;
            int tagLength = bbSize - tagStart;

            ba.Slice(tagStart, tagLength).CopyTo(tagBuffer.GetSpan(tagLength));
            tagBuffer.Advance(tagLength);

            return entry;
        }


//        private WordData2 Next()
//        {
//            ByteBuffer entryBuffer = entriesIter.Current;

//            /*
//             * Entries are typically: inflected<SEP>codedBase<SEP>tag so try to find this split.
//             */
//            byte[] ba = entryBuffer.Array;
//            int bbSize = entryBuffer.Remaining;

//            int sepPos;
//            for (sepPos = 0; sepPos < bbSize; sepPos++)
//            {
//                if (ba[sepPos] == separator)
//                {
//                    break;
//                }
//            }

//            if (sepPos == bbSize)
//            {
//                throw new Exception("Invalid dictionary " + "entry format (missing separator).");
//            }

//            wordDataStorage.Clear();
//            wordDataStorage.SetDecoder(decoder);

//            ArrayBufferWriter<byte> inflectedBuffer = wordDataStorage.WordByteBuffer;
//            Span<byte> inflectedWordBytes = inflectedBuffer.GetSpan(sepPos);
//            ba.AsSpan(0, sepPos).CopyTo(inflectedWordBytes);
//            inflectedBuffer.Advance(sepPos);

//            ArrayBufferWriter<char> inflectedCharBuffer = wordDataStorage.WordCharBuffer;
//            int inflectedCharBufferSize = decoder.GetMaxCharCount(sepPos);
//            Span<char> inflectedWordChars = inflectedCharBuffer.GetSpan(inflectedCharBufferSize);
//            int inflectedWordCharLength = decoder.GetChars(inflectedWordBytes.Slice(0, sepPos), inflectedWordChars);
//            inflectedCharBuffer.Advance(inflectedWordCharLength);
            

//            //inflectedBuffer = BufferUtils.ClearAndEnsureCapacity(inflectedBuffer, sepPos);
//            ////Array.Resize(ref inflectedBuffer, sepPos);
//            ////Array.Copy(ba, 0, inflectedBuffer, 0, sepPos);
//            //inflectedBuffer.Put(ba, 0, sepPos);
//            //inflectedBuffer.Flip();

//            //inflectedCharBuffer = BufferUtils.BytesToChars(decoder, inflectedBuffer, inflectedCharBuffer);
//            //entry.Update(inflectedBuffer, inflectedCharBuffer);





//            temp = BufferUtils.ClearAndEnsureCapacity(temp, bbSize - sepPos);
//            //Array.Resize(ref temp, bbSize - sepPos);
//            sepPos++;
//            //Array.Copy(ba, 0, temp, sepPos, bbSize - sepPos);
//            temp.Put(ba, sepPos, bbSize - sepPos);
//            temp.Flip();

//            ba = temp.Array;
//            bbSize = temp.Remaining;

//            /*
//             * Find the next separator byte's position splitting word form and tag.
//             */
//#pragma warning disable 612, 618
//            Debug.Assert(sequenceEncoder.PrefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
//            sepPos = sequenceEncoder.PrefixBytes;
//#pragma warning restore 612, 618
//            for (; sepPos < bbSize; sepPos++)
//            {
//                if (ba[sepPos] == separator)
//                    break;
//            }

//            /*
//             * Decode the stem into stem buffer.
//             */
//            ////if (decodeStems)
//            ////{
//            ////    entry.stemBuffer = sequenceEncoder.Decode(entry.stemBuffer,
//            ////                                          inflectedBuffer,
//            ////                                          ByteBuffer.Wrap(ba, 0, sepPos));
//            ////}
//            ////else
//            ////{
//            ////    entry.stemBuffer = BufferUtils.ClearAndEnsureCapacity(entry.stemBuffer, sepPos);
//            ////    entry.stemBuffer.Put(ba, 0, sepPos);
//            ////    entry.stemBuffer.Flip();
//            ////}

//            ArrayBufferWriter<byte> stemBuffer = wordDataStorage.StemByteBuffer;
//            ArrayBufferWriter<byte> tagBuffer = wordDataStorage.TagByteBuffer;

//            if (decodeStems)
//            {
//                int maxDecodedByteCount = sequenceEncoder.GetMaxDecodedByteCount(inflectedBuffer.WrittenCount, sepPos);
//                Span<byte> stemDecodedBytes = stemBuffer.GetSpan(maxDecodedByteCount);
//                if (!sequenceEncoder.TryDecode(
//                    inflectedBuffer.WrittenSpan,
//                    ba.AsSpan(0, sepPos),
//                    stemDecodedBytes,
//                    out int stemBytesWritten))
//                {
//                    throw new InvalidOperationException(
//                        "The sequence encoder produced more decoded bytes than its maximum byte count.");
//                }
//                stemBuffer.Advance(stemBytesWritten);


//                //entry.stemBuffer = BufferUtils.ClearAndEnsureCapacity(
//                //    entry.stemBuffer,
//                //    maxDecodedByteCount);

//                //if (!sequenceEncoder.TryDecode(
//                //    inflectedBuffer.Array.AsSpan(
//                //        inflectedBuffer.Position,
//                //        inflectedBuffer.WrittenCount),
//                //    ba.AsSpan(0, sepPos),
//                //    entry.stemBuffer.Array.AsSpan(0, maxDecodedByteCount),
//                //    out int bytesWritten))
//                //{
//                //    throw new InvalidOperationException(
//                //        "The sequence encoder produced more decoded bytes than its maximum byte count.");
//                //}

//                //entry.stemBuffer.Limit = bytesWritten;
//                //entry.stemBuffer.Position = 0;
//            }
//            else
//            {
//                ba.AsSpan(0, sepPos).CopyTo(stemBuffer.GetSpan(sepPos));
//                stemBuffer.Advance(sepPos);

//                //entry.stemBuffer = BufferUtils.ClearAndEnsureCapacity(entry.stemBuffer, sepPos);
//                //entry.stemBuffer.Put(ba, 0, sepPos);
//                //entry.stemBuffer.Flip();
//            }

//            // Skip separator character, if present.
//            if (sepPos + 1 <= bbSize)
//            {
//                sepPos++;
//            }

//            /*
//             * Decode the tag data.
//             */
//            int tagLength = bbSize - sepPos;
//            ba.AsSpan(sepPos, tagLength).CopyTo(tagBuffer.GetSpan(tagLength));
//            tagBuffer.Advance(tagLength);
            
//            //entry.tagBuffer = BufferUtils.ClearAndEnsureCapacity(entry.tagBuffer, bbSize - sepPos);
//            ////Array.Resize(ref entry.tagBuffer, bbSize - sepPos);
//            //entry.tagBuffer.Put(ba, sepPos, bbSize - sepPos);
//            //entry.tagBuffer.Flip();

//            return entry;
//        }



        //        private WordData Next()
        //        {
        //            ByteBuffer entryBuffer = entriesIter.Current;

        //            /*
        //             * Entries are typically: inflected<SEP>codedBase<SEP>tag so try to find this split.
        //             */
        //            byte[] ba = entryBuffer.Array;
        //            int bbSize = entryBuffer.Remaining;

        //            int sepPos;
        //            for (sepPos = 0; sepPos < bbSize; sepPos++)
        //            {
        //                if (ba[sepPos] == separator)
        //                {
        //                    break;
        //                }
        //            }

        //            if (sepPos == bbSize)
        //            {
        //                throw new Exception("Invalid dictionary " + "entry format (missing separator).");
        //            }

        //            inflectedBuffer = BufferUtils.ClearAndEnsureCapacity(inflectedBuffer, sepPos);
        //            //Array.Resize(ref inflectedBuffer, sepPos);
        //            //Array.Copy(ba, 0, inflectedBuffer, 0, sepPos);
        //            inflectedBuffer.Put(ba, 0, sepPos);
        //            inflectedBuffer.Flip();

        //            inflectedCharBuffer = BufferUtils.BytesToChars(decoder, inflectedBuffer, inflectedCharBuffer);
        //            entry.Update(inflectedBuffer, inflectedCharBuffer);

        //            temp = BufferUtils.ClearAndEnsureCapacity(temp, bbSize - sepPos);
        //            //Array.Resize(ref temp, bbSize - sepPos);
        //            sepPos++;
        //            //Array.Copy(ba, 0, temp, sepPos, bbSize - sepPos);
        //            temp.Put(ba, sepPos, bbSize - sepPos);
        //            temp.Flip();

        //            ba = temp.Array;
        //            bbSize = temp.Remaining;

        //            /*
        //             * Find the next separator byte's position splitting word form and tag.
        //             */
        //#pragma warning disable 612, 618
        //            Debug.Assert(sequenceEncoder.PrefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
        //            sepPos = sequenceEncoder.PrefixBytes;
        //#pragma warning restore 612, 618
        //            for (; sepPos < bbSize; sepPos++)
        //            {
        //                if (ba[sepPos] == separator)
        //                    break;
        //            }

        //            /*
        //             * Decode the stem into stem buffer.
        //             */
        //            ////if (decodeStems)
        //            ////{
        //            ////    entry.stemBuffer = sequenceEncoder.Decode(entry.stemBuffer,
        //            ////                                          inflectedBuffer,
        //            ////                                          ByteBuffer.Wrap(ba, 0, sepPos));
        //            ////}
        //            ////else
        //            ////{
        //            ////    entry.stemBuffer = BufferUtils.ClearAndEnsureCapacity(entry.stemBuffer, sepPos);
        //            ////    entry.stemBuffer.Put(ba, 0, sepPos);
        //            ////    entry.stemBuffer.Flip();
        //            ////}

        //            if (decodeStems)
        //            {
        //                int maxDecodedByteCount =
        //                    sequenceEncoder.GetMaxDecodedByteCount(
        //                        inflectedBuffer.Remaining,
        //                        sepPos);

        //                entry.stemBuffer = BufferUtils.ClearAndEnsureCapacity(
        //                    entry.stemBuffer,
        //                    maxDecodedByteCount);

        //                if (!sequenceEncoder.TryDecode(
        //                    inflectedBuffer.Array.AsSpan(
        //                        inflectedBuffer.Position,
        //                        inflectedBuffer.Remaining),
        //                    ba.AsSpan(0, sepPos),
        //                    entry.stemBuffer.Array.AsSpan(0, maxDecodedByteCount),
        //                    out int bytesWritten))
        //                {
        //                    throw new InvalidOperationException(
        //                        "The sequence encoder produced more decoded bytes than its maximum byte count.");
        //                }

        //                entry.stemBuffer.Limit = bytesWritten;
        //                entry.stemBuffer.Position = 0;
        //            }
        //            else
        //            {
        //                entry.stemBuffer = BufferUtils.ClearAndEnsureCapacity(entry.stemBuffer, sepPos);
        //                entry.stemBuffer.Put(ba, 0, sepPos);
        //                entry.stemBuffer.Flip();
        //            }

        //            // Skip separator character, if present.
        //            if (sepPos + 1 <= bbSize)
        //            {
        //                sepPos++;
        //            }

        //            /*
        //             * Decode the tag data.
        //             */
        //            entry.tagBuffer = BufferUtils.ClearAndEnsureCapacity(entry.tagBuffer, bbSize - sepPos);
        //            //Array.Resize(ref entry.tagBuffer, bbSize - sepPos);
        //            entry.tagBuffer.Put(ba, sepPos, bbSize - sepPos);
        //            entry.tagBuffer.Flip();

        //            return entry;
        //        }

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
