using J2N.IO;
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
        private readonly IEnumerator<ByteBuffer> entriesIter;
        private readonly WordData entry;
        private readonly byte separator;
        private readonly bool decodeStems;

        private ByteBuffer inflectedBuffer = ByteBuffer.Allocate(0);
        private CharBuffer inflectedCharBuffer = CharBuffer.Allocate(0);
        private ByteBuffer temp = ByteBuffer.Allocate(0);
        private readonly ISequenceEncoder sequenceEncoder;

        private WordData? current;

        /// <summary>
        /// Initializes a new instance of <see cref="DictionaryEnumerator"/>.
        /// </summary>
        public DictionaryEnumerator(Dictionary dictionary, Encoding decoder, bool decodeStems)
        {
            this.entriesIter = dictionary.FSA.GetEnumerator();
            this.separator = dictionary.Metadata.Separator;
            this.sequenceEncoder = dictionary.Metadata.SequenceEncoderType.Get();
            this.decoder = decoder;
            this.entry = new WordData(decoder);
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
            ByteBuffer entryBuffer = entriesIter.Current;

            /*
             * Entries are typically: inflected<SEP>codedBase<SEP>tag so try to find this split.
             */
            byte[] ba = entryBuffer.Array;
            int bbSize = entryBuffer.Remaining;

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

            inflectedBuffer = BufferUtils.ClearAndEnsureCapacity(inflectedBuffer, sepPos);
            //Array.Resize(ref inflectedBuffer, sepPos);
            //Array.Copy(ba, 0, inflectedBuffer, 0, sepPos);
            inflectedBuffer.Put(ba, 0, sepPos);
            inflectedBuffer.Flip();

            inflectedCharBuffer = BufferUtils.BytesToChars(decoder, inflectedBuffer, inflectedCharBuffer);
            entry.Update(inflectedBuffer, inflectedCharBuffer);

            temp = BufferUtils.ClearAndEnsureCapacity(temp, bbSize - sepPos);
            //Array.Resize(ref temp, bbSize - sepPos);
            sepPos++;
            //Array.Copy(ba, 0, temp, sepPos, bbSize - sepPos);
            temp.Put(ba, sepPos, bbSize - sepPos);
            temp.Flip();

            ba = temp.Array;
            bbSize = temp.Remaining;

            /*
             * Find the next separator byte's position splitting word form and tag.
             */
#pragma warning disable 612, 618
            Debug.Assert(sequenceEncoder.PrefixBytes <= bbSize, sequenceEncoder.GetType() + " >? " + bbSize);
            sepPos = sequenceEncoder.PrefixBytes;
#pragma warning restore 612, 618
            for (; sepPos < bbSize; sepPos++)
            {
                if (ba[sepPos] == separator)
                    break;
            }

            /*
             * Decode the stem into stem buffer.
             */
            if (decodeStems)
            {
                entry.stemBuffer = sequenceEncoder.Decode(entry.stemBuffer,
                                                      inflectedBuffer,
                                                      ByteBuffer.Wrap(ba, 0, sepPos));
            }
            else
            {
                entry.stemBuffer = BufferUtils.ClearAndEnsureCapacity(entry.stemBuffer, sepPos);
                entry.stemBuffer.Put(ba, 0, sepPos);
                entry.stemBuffer.Flip();
            }

            // Skip separator character, if present.
            if (sepPos + 1 <= bbSize)
            {
                sepPos++;
            }

            /*
             * Decode the tag data.
             */
            entry.tagBuffer = BufferUtils.ClearAndEnsureCapacity(entry.tagBuffer, bbSize - sepPos);
            //Array.Resize(ref entry.tagBuffer, bbSize - sepPos);
            entry.tagBuffer.Put(ba, sepPos, bbSize - sepPos);
            entry.tagBuffer.Flip();

            return entry;
        }

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
