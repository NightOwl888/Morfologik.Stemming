using System;
using System.Diagnostics;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Represents the word data produced by a dictionary lookup.
    /// </summary>
    public sealed class WordData2
    {
        /// <summary>
        /// Error information if somebody puts us in a .NET collection.
        /// </summary>
        private const string CollectionsErrorMessage = "Not suitable for use"
            + " in .NET collections framework (volatile content). Refer to documentation.";

        private readonly IWordDataStorage storage;
        private int index = 0;

        internal WordData2(IWordDataStorage storage)
        {
            Debug.Assert(storage is not null);
            this.storage = storage!;
        }

        /// <summary>
        /// Gets the inflected word from the underlying data storage.
        /// Usually the parameter passed to
        /// <see cref="DictionaryLookup.Lookup(ReadOnlySpan{char}, DictionaryLookupResult?)"/>.
        /// </summary>
        /// <remarks>
        /// This property provides direct access to the underlying memory
        /// of the storage. The caller is responsible for ensuring
        /// that the data is either copied to an external buffer or
        /// that the underlying storage stay in scope as long as they
        /// need to access it.
        /// </remarks>
        public ReadOnlyMemory<char> Word => storage.Word;

        /// <summary>
        /// Gets the decoded stem associated with the word.
        /// Will be <see cref="ReadOnlyMemory{T}.Empty"/> if no stem exists.
        /// </summary>
        /// <remarks>
        /// This property provides direct access to the underlying memory
        /// of the storage. The caller is responsible for ensuring
        /// that the data is either copied to an external buffer or
        /// that the underlying storage stay in scope as long as they
        /// need to access it.
        /// </remarks>
        public ReadOnlyMemory<char> Stem => storage.GetStem(index);

        /// <summary>
        /// Gets the decoded tag associated with the word.
        /// Will be <see cref="ReadOnlyMemory{T}.Empty"/> if no tag exists.
        /// </summary>
        /// <remarks>
        /// This property provides direct access to the underlying memory
        /// of the storage. The caller is responsible for ensuring
        /// that the data is either copied to an external buffer or
        /// that the underlying storage stay in scope as long as they
        /// need to access it.
        /// </remarks>
        public ReadOnlyMemory<char> Tag => storage.GetTag(index);

        /// <summary>
        /// Gets the word's binary data (no charset decoding).
        /// </summary>
        /// <remarks>
        /// This property provides direct access to the underlying memory
        /// of the storage. The caller is responsible for ensuring
        /// that the data is either copied to an external buffer or
        /// that the underlying storage stay in scope as long as they
        /// need to access it.
        /// </remarks>
        public ReadOnlyMemory<byte> WordBytes => storage.WordBytes;

        /// <summary>
        /// Gets the stem's binary data (no charset decoding).
        /// </summary>
        /// <remarks>
        /// This property provides direct access to the underlying memory
        /// of the storage. The caller is responsible for ensuring
        /// that the data is either copied to an external buffer or
        /// that the underlying storage stay in scope as long as they
        /// need to access it.
        /// </remarks>
        public ReadOnlyMemory<byte> StemBytes => storage.GetStemBytes(index);

        /// <summary>
        /// Gets the tag's binary data (no charset decoding).
        /// </summary>
        /// <remarks>
        /// This property provides direct access to the underlying memory
        /// of the storage. The caller is responsible for ensuring
        /// that the data is either copied to an external buffer or
        /// that the underlying storage stay in scope as long as they
        /// need to access it.
        /// </remarks>
        public ReadOnlyMemory<byte> TagBytes => storage.GetTagBytes(index);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            throw new NotSupportedException(CollectionsErrorMessage);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            throw new NotSupportedException(CollectionsErrorMessage);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"WordData[{Word},{Stem},{Tag}]";
        }

        internal void SetIndex(int index)
        {
            this.index = index;
        }

        // TODO: Implement Clone()
    }
}
