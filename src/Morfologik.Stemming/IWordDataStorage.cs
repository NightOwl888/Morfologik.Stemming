using System;

namespace Morfologik.Stemming
{
    internal interface IWordDataStorage
    {
        ReadOnlyMemory<char> Word { get; }

        

        //ArrayBufferWriter<char> StemCharBuffer { get; }

        //ArrayBufferWriter<char> TagCharBuffer { get; }

        //Encoding? Decoder { get; }



        ReadOnlyMemory<char> GetStem(int index);

        ReadOnlyMemory<char> GetTag(int index);

        ReadOnlyMemory<byte> WordBytes { get; }

        ReadOnlyMemory<byte> GetStemBytes(int index);

        ReadOnlyMemory<byte> GetTagBytes(int index);

        //void SetDecoder(Encoding decoder);

        //void SetWord(ReadOnlySpan<char> word);

        //void Clear();
    }
}
