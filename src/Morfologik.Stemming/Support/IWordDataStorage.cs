using System;
using System.Text;

namespace Morfologik.Stemming
{
    internal interface IWordDataStorage
    {
        Encoding Decoder { get; }


        ReadOnlyMemory<char> Word { get; }

        ReadOnlyMemory<byte> WordBytes { get; }


        ReadOnlyMemory<char> GetStem(int index);

        ReadOnlyMemory<byte> GetStemBytes(int index);

        ArrayBufferWriter<char> StemCharBuffer { get; }

        void SetStemOffsets(int index, int offset, int length);

        bool IsStemLoaded(int index);


        ReadOnlyMemory<char> GetTag(int index);

        ReadOnlyMemory<byte> GetTagBytes(int index);

        ArrayBufferWriter<char> TagCharBuffer { get; }

        void SetTagOffsets(int index, int offset, int length);

        bool IsTagLoaded(int index);


        //void SetDecoder(Encoding decoder);

        //void SetWord(ReadOnlySpan<char> word);

        //void Clear();
    }
}
