using System;
using System.Text;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Contract for the data storage of a <see cref="WordData"/> instance.
    /// <para/>
    /// This contract allows for either single-element or multiple-element
    /// backing stores that are indexed. It also provides direct access
    /// to the <see cref="StemCharBuffer"/> and <see cref="TagCharBuffer"/>
    /// along with the other members required to lazily load <see cref="WordData.Stem"/>
    /// and <see cref="WordData.Tag"/>.
    /// </summary>
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
    }
}
