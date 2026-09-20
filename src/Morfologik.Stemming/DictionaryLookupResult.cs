using System;
using System.Collections;
using System.Collections.Generic;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Contains the results of a dictionary lookup in buffer that can be
    /// reused for additional lookup operations.
    /// </summary>
    public sealed class DictionaryLookupResult : IEnumerable<WordData2>
    {
        private struct Entry
        {
            public int StemOffset;
            public int StemLength;
            public int TagOffset;
            public int TagLength;
        }

        private readonly ArrayBufferWriter<char> wordBuffer;
        private readonly ArrayBufferWriter<char> stemBuffer;
        private readonly ArrayBufferWriter<char> tagBuffer;
        private readonly ArrayBufferWriter<Entry> entries;

        public DictionaryLookupResult()
        {
            wordBuffer = new ArrayBufferWriter<char>();
            stemBuffer = new ArrayBufferWriter<char>();
            tagBuffer = new ArrayBufferWriter<char>();
            entries = new ArrayBufferWriter<Entry>();
        }

        /// <summary>
        /// Gets the number of results in this lookup result.
        /// </summary>
        public int Count => entries.WrittenCount;

        /// <summary>
        /// Gets the word associated with this lookup.
        /// </summary>
        public ReadOnlyMemory<char> Word => wordBuffer.WrittenMemory;

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<WordData2> IEnumerable<WordData2>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<WordData2>
        {
            private readonly DictionaryLookupResult result;
            private readonly LookupWordData wordData;
            private int index;

            internal Enumerator(DictionaryLookupResult result)
            {
                this.result = result;
                wordData = new LookupWordData(result);
                index = -1;
            }

            public WordData2 Current => wordData;

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

        internal ArrayBufferWriter<char> StemBuffer => stemBuffer;
        internal ArrayBufferWriter<char> TagBuffer => tagBuffer;

        internal ReadOnlyMemory<char> GetStem(int index)
        {
            Entry entry = entries.WrittenSpan[index];
            return StemBuffer.WrittenMemory.Slice(entry.StemOffset, entry.StemLength);
        }

        internal ReadOnlyMemory<char> GetTag(int index)
        {
            Entry entry = entries.WrittenSpan[index];
            return TagBuffer.WrittenMemory.Slice(entry.TagOffset, entry.TagLength);
        }

        /// <summary>
        /// Removes all results while retaining the buffers' allocated storage.
        /// </summary>
        internal void Clear()
        {
            wordBuffer.Clear();
            stemBuffer.Clear();
            tagBuffer.Clear();
            entries.Clear();
        }

        internal void SetWord(ReadOnlySpan<char> word)
        {
            Span<char> temp = wordBuffer.GetSpan(word.Length);
            word.CopyTo(temp);
            wordBuffer.Advance(word.Length);
        }

        internal void AddEntry(int stemOffset, int stemLength, int tagOffset, int tagLength)
        {
            Span<Entry> destination = entries.GetSpan(1);

            destination[0] = new Entry
            {
                StemOffset = stemOffset,
                StemLength = stemLength,
                TagOffset = tagOffset,
                TagLength = tagLength
            };

            entries.Advance(1);
        }
    }
}
