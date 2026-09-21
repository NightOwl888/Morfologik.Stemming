using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Contains the results of a dictionary lookup in buffer that can be
    /// reused for additional lookup operations.
    /// </summary>
    public sealed class DictionaryLookupResult : IWordDataStorage, IEnumerable<WordData>
    {
        private struct Entry
        {
            public bool IsStemLoaded;
            public int StemCharsOffset;
            public int StemCharsLength;

            public bool IsTagLoaded;
            public int TagCharsOffset;
            public int TagCharsLength;

            public int StemBytesOffset;
            public int StemBytesLength;
            public int TagBytesOffset;
            public int TagBytesLength;
        }

        private readonly ArrayBufferWriter<char> wordCharsBuffer;
        private readonly ArrayBufferWriter<char> stemCharsBuffer;
        private readonly ArrayBufferWriter<char> tagCharsBuffer;

        private readonly ArrayBufferWriter<byte> wordBytesBuffer;
        private readonly ArrayBufferWriter<byte> stemBytesBuffer;
        private readonly ArrayBufferWriter<byte> tagBytesBuffer;
        private readonly ArrayBufferWriter<Entry> entries;

        private Encoding? decoder;

        public DictionaryLookupResult()
        {
            wordCharsBuffer = new ArrayBufferWriter<char>();
            stemCharsBuffer = new ArrayBufferWriter<char>();
            tagCharsBuffer = new ArrayBufferWriter<char>();
            wordBytesBuffer = new ArrayBufferWriter<byte>();
            stemBytesBuffer = new ArrayBufferWriter<byte>();
            tagBytesBuffer = new ArrayBufferWriter<byte>();
            entries = new ArrayBufferWriter<Entry>();
        }

        /// <summary>
        /// Removes all results while retaining the buffers' allocated storage.
        /// </summary>
        internal void Clear()
        {
            wordCharsBuffer.Clear();
            stemCharsBuffer.Clear();
            tagCharsBuffer.Clear();
            wordBytesBuffer.Clear();
            stemBytesBuffer.Clear();
            tagBytesBuffer.Clear();
            entries.Clear();
            decoder = null;
        }

        /// <summary>
        /// Gets the number of results in this lookup result.
        /// </summary>
        public int Count => entries.WrittenCount;

        /// <summary>
        /// Gets the word associated with this lookup.
        /// </summary>
        public ReadOnlyMemory<char> Word => wordCharsBuffer.WrittenMemory;

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<WordData> IEnumerable<WordData>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<WordData>
        {
            private readonly DictionaryLookupResult result;
            private readonly WordData wordData;
            private int index;

            internal Enumerator(DictionaryLookupResult result)
            {
                this.result = result;
                wordData = new WordData(result);
                index = -1;
            }

            public WordData Current => wordData;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                int nextIndex = index + 1;
                if (nextIndex >= result.Count)
                    return false;

                index = nextIndex;
                wordData.SetIndex(index);
                return true;
            }

            public void Reset()
            {
                index = -1;
            }

            public void Dispose()
            {
                // Intentionally empty
            }
        }

        internal ArrayBufferWriter<char> WordCharsBuffer => wordCharsBuffer;
        //internal ArrayBufferWriter<char> StemCharsBuffer => stemCharsBuffer;
        //internal ArrayBufferWriter<char> TagCharsBuffer => tagCharsBuffer;
        internal ArrayBufferWriter<byte> WordBytesBuffer => wordBytesBuffer;
        internal ArrayBufferWriter<byte> StemBytesBuffer => stemBytesBuffer;
        internal ArrayBufferWriter<byte> TagBytesBuffer => tagBytesBuffer;


        internal ReadOnlyMemory<char> GetStem(int index)
        {
            Entry entry = entries.WrittenSpan[index];
            return stemCharsBuffer.WrittenMemory.Slice(entry.StemCharsOffset, entry.StemCharsLength);
        }

        internal void SetStemOffsets(int index, int offset, int length)
        {
            ref Entry entry = ref entries.GetReference(index);
            entry.StemCharsOffset = offset;
            entry.StemCharsLength = length;
            entry.IsStemLoaded = true;
        }

        internal bool IsStemLoaded(int index)
        {
            Entry entry = entries.WrittenSpan[index];
            return entry.IsStemLoaded;
        }

        internal ReadOnlyMemory<char> GetTag(int index)
        {
            Entry entry = entries.WrittenSpan[index];
            return tagCharsBuffer.WrittenMemory.Slice(entry.TagCharsOffset, entry.TagCharsLength);
        }

        internal void SetTagOffsets(int index, int offset, int length)
        {
            ref Entry entry = ref entries.GetReference(index);
            entry.TagCharsOffset = offset;
            entry.TagCharsLength = length;
            entry.IsTagLoaded = true;
        }

        internal bool IsTagLoaded(int index)
        {
            Entry entry = entries.WrittenSpan[index];
            return entry.IsTagLoaded;
        }

        internal ReadOnlyMemory<byte> GetStemBytes(int index)
        {
            Entry entry = entries.WrittenSpan[index];
            return stemBytesBuffer.WrittenMemory.Slice(entry.StemBytesOffset, entry.StemBytesLength);
        }

        internal ReadOnlyMemory<byte> GetTagBytes(int index)
        {
            Entry entry = entries.WrittenSpan[index];
            return tagBytesBuffer.WrittenMemory.Slice(entry.TagBytesOffset, entry.TagBytesLength);
        }

        internal void SetWord(ReadOnlySpan<char> word)
        {
            Span<char> temp = wordCharsBuffer.GetSpan(word.Length);
            word.CopyTo(temp);
            wordCharsBuffer.Advance(word.Length);
        }

        internal void SetDecoder(Encoding decoder)
        {
            Debug.Assert(decoder is not null);
            this.decoder = decoder;
        }

        internal Encoding Decoder
        {
            get
            {
                Debug.Assert(decoder is not null);
                return decoder!;
            }
        }

        internal void AddEntry(int stemBytesOffset, int stemBytesLength, int tagBytesOffset, int tagBytesLength)
        {
            Span<Entry> destination = entries.GetSpan(1);

            destination[0] = new Entry
            {
                StemBytesOffset = stemBytesOffset,
                StemBytesLength = stemBytesLength,
                TagBytesOffset = tagBytesOffset,
                TagBytesLength = tagBytesLength
            };

            entries.Advance(1);
        }

        #region IWordDataStorage Members

        Encoding IWordDataStorage.Decoder => Decoder;


        ReadOnlyMemory<char> IWordDataStorage.Word => wordCharsBuffer.WrittenMemory;

        ReadOnlyMemory<byte> IWordDataStorage.WordBytes => wordBytesBuffer.WrittenMemory;


        ReadOnlyMemory<char> IWordDataStorage.GetStem(int index)
            => GetStem(index);

        ReadOnlyMemory<byte> IWordDataStorage.GetStemBytes(int index)
            => GetStemBytes(index);

        ArrayBufferWriter<char> IWordDataStorage.StemCharBuffer => stemCharsBuffer;

        void IWordDataStorage.SetStemOffsets(int index, int offset, int length)
            => SetStemOffsets(index, offset, length);

        bool IWordDataStorage.IsStemLoaded(int index)
            => IsStemLoaded(index);


        ReadOnlyMemory<char> IWordDataStorage.GetTag(int index)
            => GetTag(index);

        ReadOnlyMemory<byte> IWordDataStorage.GetTagBytes(int index)
            => GetTagBytes(index);

        ArrayBufferWriter<char> IWordDataStorage.TagCharBuffer => tagCharsBuffer;

        void IWordDataStorage.SetTagOffsets(int index, int offset, int length)
            => SetTagOffsets(index, offset, length);

        bool IWordDataStorage.IsTagLoaded(int index)
            => IsTagLoaded(index);

        #endregion IWordDataStorage Members
    }
}
