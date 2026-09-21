using System;
using System.Diagnostics;
using System.Text;

namespace Morfologik.Stemming
{
    internal sealed class WordDataStorage : IWordDataStorage
    {
        public WordDataStorage()
        {
        }

        public WordDataStorage(IWordDataStorage toCopy, int index)
        {
            decoder = toCopy.Decoder;

            ReadOnlySpan<char> word = toCopy.Word.Span;
            word.CopyTo(wordCharBuffer.GetSpan(word.Length));
            wordCharBuffer.Advance(word.Length);

            ReadOnlySpan<byte> wordBytes = toCopy.WordBytes.Span;
            wordBytes.CopyTo(wordByteBuffer.GetSpan(wordBytes.Length));
            wordByteBuffer.Advance(wordBytes.Length);

            ReadOnlySpan<byte> stemBytes = toCopy.GetStemBytes(index).Span;
            stemBytes.CopyTo(stemByteBuffer.GetSpan(stemBytes.Length));
            stemByteBuffer.Advance(stemBytes.Length);

            ReadOnlySpan<byte> tagBytes = toCopy.GetTagBytes(index).Span;
            tagBytes.CopyTo(tagByteBuffer.GetSpan(tagBytes.Length));
            tagByteBuffer.Advance(tagBytes.Length);
        }

        private readonly ArrayBufferWriter<char> wordCharBuffer = new();
        private readonly ArrayBufferWriter<byte> wordByteBuffer = new();

        private readonly ArrayBufferWriter<char> stemCharBuffer = new();
        private readonly ArrayBufferWriter<byte> stemByteBuffer = new();

        private readonly ArrayBufferWriter<char> tagCharBuffer = new();
        private readonly ArrayBufferWriter<byte> tagByteBuffer = new();

        private Encoding? decoder = null;
        private bool isStemLoaded = false;
        private bool isTagLoaded = false;

        internal void Clear()
        {
            wordCharBuffer.Clear();
            wordByteBuffer.Clear();
            stemCharBuffer.Clear();
            stemByteBuffer.Clear();
            tagCharBuffer.Clear();
            tagByteBuffer.Clear();
            decoder = null;
            isStemLoaded = false;
            isTagLoaded = false;
        }
        internal void SetDecoder(Encoding decoder)
        {
            Debug.Assert(decoder is not null);
            this.decoder = decoder;
        }

        public Encoding Decoder
        {
            get
            {
                Debug.Assert(decoder is not null);
                return decoder!;
            }
        }

        public ReadOnlyMemory<char> Word => wordCharBuffer.WrittenMemory;

        public ReadOnlyMemory<byte> WordBytes => wordByteBuffer.WrittenMemory;

        public ArrayBufferWriter<char> StemCharBuffer => stemCharBuffer;

        public ArrayBufferWriter<char> TagCharBuffer => tagCharBuffer;

        public ReadOnlyMemory<char> GetStem(int index)
        {
            Debug.Assert(index == 0);
            return stemCharBuffer.WrittenMemory;
        }

        public ReadOnlyMemory<byte> GetStemBytes(int index)
        {
            Debug.Assert(index == 0);
            return stemByteBuffer.WrittenMemory;
        }

        public ReadOnlyMemory<char> GetTag(int index)
        {
            Debug.Assert(index == 0);
            return tagCharBuffer.WrittenMemory;
        }

        public ReadOnlyMemory<byte> GetTagBytes(int index)
        {
            Debug.Assert(index == 0);
            return tagByteBuffer.WrittenMemory;
        }

        public bool IsStemLoaded(int index)
        {
            Debug.Assert(index == 0);
            return isStemLoaded;
        }

        public bool IsTagLoaded(int index)
        {
            Debug.Assert(index == 0);
            return isTagLoaded;
        }

        public void SetStemOffsets(int index, int offset, int length)
        {
            Debug.Assert(index == 0);
            isStemLoaded = true;
        }

        public void SetTagOffsets(int index, int offset, int length)
        {
            Debug.Assert(index == 0);
            isTagLoaded = true;
        }
    }
}
