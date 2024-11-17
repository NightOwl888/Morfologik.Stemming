using J2N.IO;
using J2N.Text;
using System;
using System.Text;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Stem and tag data associated with a given word.
    /// <para/>
    /// Instances of this class are reused and mutable (values
    /// returned from <see cref="GetStem()"/>, <see cref="Word"/>
    /// and other related methods change on subsequent calls to 
    /// <see cref="DictionaryLookup"/> class that returned a given
    /// instance of <see cref="WordData"/>.
    /// <para/>
    /// If you need a copy of the
    /// stem or tag data for a given word, you have to create a custom buffer
    /// yourself and copy the associated data, perform <see cref="Clone()"/> or create
    /// strings (they are immutable) using <see cref="GetStem()"/> and then
    /// <see cref="ICharSequence.ToString()"/>.
    /// <para/>
    /// For reasons above it makes no sense to use instances
    /// of this class in associative containers or lists. In fact,
    /// both <see cref="Equals(object)"/> and <see cref="GetHashCode()"/> are
    /// overridden and throw exceptions to prevent accidental damage.
    /// </summary>
    public sealed class WordData
#if FEATURE_CLONEABLE
        : ICloneable
#endif
    {
        /// <summary>
        /// Error information if somebody puts us in a .NET collection.
        /// </summary>
        private const string CollectionsErrorMessage = "Not suitable for use"
            + " in .NET collections framework (volatile content). Refer to documentation.";

        /// <summary>Character encoding in internal buffers.</summary>
        private readonly Encoding decoder;

        /// <summary>
        /// Inflected word form data.
        /// </summary>
        private ICharSequence? wordCharSequence;

        /// <summary>
        /// Character sequence after converting <see cref="stemBuffer"/> using
        /// <see cref="decoder"/>.
        /// </summary>
        private CharBuffer stemCharSequence;

        /// <summary>
        /// Character sequence after converting <see cref="tagBuffer"/> using
        /// <see cref="decoder"/>.
        /// </summary>
        private CharBuffer tagCharSequence;

        /// <summary>Byte buffer holding the inflected word form data.</summary>
        internal ByteBuffer? wordBuffer;

        /// <summary>Byte buffer holding stem data.</summary>
        internal ByteBuffer stemBuffer;

        /// <summary>Byte buffer holding tag data.</summary>
        internal ByteBuffer tagBuffer;

        /// <summary>
        /// Internal scope constructor.
        /// </summary>
        internal WordData(Encoding decoder)
        {
            this.decoder = decoder;

            stemBuffer = ByteBuffer.Allocate(0);
            tagBuffer = ByteBuffer.Allocate(0);
            stemCharSequence = CharBuffer.Allocate(0);
            tagCharSequence = CharBuffer.Allocate(0);
        }

        /// <summary>
        /// A constructor for tests only.
        /// </summary>
        internal WordData(string stem, string tag, string encoding)
            : this(Encoding.GetEncoding(encoding))
        {
            if (stem != null)
                stemBuffer.Put(this.decoder.GetBytes(stem));
            if (tag != null)
                tagBuffer.Put(this.decoder.GetBytes(tag));
        }

        /// <summary>
        /// Copy the stem's binary data (no charset decoding) to a custom byte
        /// buffer.
        /// <para/>
        /// The buffer is cleared prior to copying and flipped for reading
        /// upon returning from this method. If the buffer is null or not large
        /// enough to hold the result, a new buffer is allocated.
        /// </summary>
        /// <param name="target">Target byte buffer to copy the stem buffer to or
        /// <c>null</c> if a new buffer should be allocated.</param>
        /// <returns>Returns <paramref name="target"/> or the new reallocated buffer.</returns>
        public ByteBuffer GetStemBytes(ByteBuffer? target)
        {
            target = BufferUtils.ClearAndEnsureCapacity(target, stemBuffer.Remaining);
            stemBuffer.Mark();
            target.Put(stemBuffer);
            stemBuffer.Reset();
            target.Flip();
            return target;
        }

        /// <summary>
        /// Copy the tag's binary data (no charset decoding) to a custom byte buffer.
        /// <para/>
        /// The buffer is cleared prior to copying and flipped for reading
        /// upon returning from this method. If the buffer is <c>null</c> or not large
        /// enough to hold the result, a new buffer is allocated.
        /// </summary>
        /// <param name="target">Target byte buffer to copy the tag buffer to or
        /// <c>null</c> if a new buffer should be allocated.</param>
        /// <returns>Returns <paramref name="target"/> or the new reallocated buffer.</returns>
        public ByteBuffer GetTagBytes(ByteBuffer? target)
        {
            target = BufferUtils.ClearAndEnsureCapacity(target, tagBuffer.Remaining);
            tagBuffer.Mark();
            target.Put(tagBuffer);
            tagBuffer.Reset();
            target.Flip();
            return target;
        }

        /// <summary>
        /// Copy the inflected word's binary data (no charset decoding) to a custom
        /// byte buffer.
        /// <para/>
        /// The buffer is cleared prior to copying and flipped for reading
        /// upon returning from this method. If the buffer is null or not large
        /// enough to hold the result, a new buffer is allocated.
        /// </summary>
        /// <param name="target">Target byte buffer to copy the word buffer to or
        /// <c>null</c> if a new buffer should be allocated.</param>
        /// <returns>Returns <paramref name="target"/> or the new reallocated buffer.</returns>
        public ByteBuffer GetWordBytes(ByteBuffer? target)
        {
            // .NET specific - throw sensible exception if wordBuffer is null
            if (wordBuffer is null)
                throw new InvalidOperationException("wordBuffer must be set prior to calling GetWordBytes(ByteBuffer)");

            target = BufferUtils.ClearAndEnsureCapacity(target, wordBuffer.Remaining);
            wordBuffer.Mark();
            target.Put(wordBuffer);
            wordBuffer.Reset();
            target.Flip();
            return target;
        }

        /// <summary>
        /// Return tag data decoded to a character sequence or
        /// <c>null</c> if no associated tag data exists.
        /// </summary>
        public ICharSequence? GetTag()
        {
            //decoder.GetChars(tagBuffer, 0, tagBuffer.Length, tagCharSequence, 0);
            //return tagCharSequence.Length == 0 ? null : new string(tagCharSequence);
            tagCharSequence = BufferUtils.BytesToChars(decoder, tagBuffer, tagCharSequence);
            return tagCharSequence.Length == 0 ? null : tagCharSequence.ToString().AsCharSequence();
        }

        /// <summary>
        /// Return stem data decoded to a character sequence or
        /// <c>null</c> if no associated stem data exists.
        /// </summary>
        public ICharSequence? GetStem()
        {
            //decoder.GetChars(stemBuffer, 0, stemBuffer.Length, stemCharSequence, 0);
            //return stemCharSequence.Length == 0 ? null : new string(stemCharSequence);

            stemCharSequence = BufferUtils.BytesToChars(decoder, stemBuffer, stemCharSequence);
            return stemCharSequence.Length == 0 ? null : stemCharSequence.ToString().AsCharSequence();
        }

        /// <summary>
        /// Return inflected word form data. Usually the parameter passed to
        /// <see cref="DictionaryLookup.Lookup(ICharSequence)"/>.
        /// </summary>
        public ICharSequence? Word => wordCharSequence;

        /// <summary>
        /// 
        /// </summary>
        public override bool Equals(object? obj)
        {
            throw new NotSupportedException(CollectionsErrorMessage);
        }

        /// <summary>
        /// 
        /// </summary>
        public override int GetHashCode()
        {
            throw new NotSupportedException(CollectionsErrorMessage);
        }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {
            return $"WordData[{Word},{GetStem()},{GetTag()}]";
        }

        /// <summary>
        /// Declare a covariant of Clone() that returns a deep copy of
        /// this object. The content of all internal buffers is copied.
        /// </summary>
        public object Clone()
        {
            return new WordData(this.decoder)
            {
                wordCharSequence = CloneCharSequence(wordCharSequence),
                wordBuffer = GetWordBytes(null),
                stemBuffer = GetStemBytes(null),
                tagBuffer = GetTagBytes(null)
            };
        }

        /// <summary>
        /// Clone char sequences only if not immutable.
        /// </summary>
        private ICharSequence CloneCharSequence(ICharSequence? chs)
        {
            // .NET specific - if the source is null, return a null char sequence
            if (chs is null)
                return new StringCharSequence(null);
            if (chs is StringCharSequence)
                return chs;
            return chs.ToString().AsCharSequence();
        }

        internal void Update(ByteBuffer wordBuffer, ICharSequence word)
        {
            this.stemCharSequence.Clear();
            this.tagCharSequence.Clear();
            this.stemBuffer.Clear();
            this.tagBuffer.Clear();

            this.wordBuffer = wordBuffer;
            this.wordCharSequence = word;
        }

        internal void Update(ByteBuffer wordBuffer, char[] word) => Update(wordBuffer, word.AsCharSequence());

        internal void Update(ByteBuffer wordBuffer, StringBuilder word) => Update(wordBuffer, word.AsCharSequence());

        internal void Update(ByteBuffer wordBuffer, string word) => Update(wordBuffer, word.AsCharSequence());
    }
}
