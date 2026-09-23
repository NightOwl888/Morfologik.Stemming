using Morfologik.Fsa;
using Morfologik.Stemming.Support;
using System.IO;

namespace Morfologik.Stemming
{
    /// <summary>
    /// A dictionary combines <see cref="Fsa.FSA"/> automaton and <see cref="DictionaryMetadata"/>
    /// describing the way terms are encoded in the automaton.
    /// <para/>
    /// A dictionary consists of two files:
    /// <list type="bullet">
    ///     <item><description>an actual compressed FSA file,</description></item>
    ///     <item><description><see cref="DictionaryMetadata"/>, describing the way terms are encoded.</description></item>
    /// </list>
    /// </summary>
    public sealed class Dictionary
    {
        /// <summary>
        /// <see cref="Fsa.FSA"/> automaton with the compiled dictionary data.
        /// </summary>
        public FSA FSA { get; }

        /// <summary>
        /// Metadata associated with the dictionary.
        /// </summary>
        public DictionaryMetadata Metadata { get; }

        /// <summary>
        /// It is strongly recommended to use static methods in this class for
        /// reading dictionaries.
        /// </summary>
        /// <param name="fsa">An instantiated <see cref="Morfologik.Fsa.FSA"/> instance.</param>
        /// <param name="metadata">
        /// A map of attributes describing the compression format and
        /// other settings not contained in the FSA automaton. For an
        /// explanation of available attributes and their possible values,
        /// see <see cref="DictionaryMetadata"/>.
        /// </param>
        public Dictionary(FSA fsa, DictionaryMetadata metadata)
        {
            this.FSA = fsa;
            this.Metadata = metadata;
        }

        static Dictionary()
        {
            EncodingProviderInitializer.EnsureInitialized(); // Morfologik.Stemming specific - initialize encoding provider
        }

        /// <summary>
        /// Attempts to load a dictionary using the path to the FSA file and the
        /// expected metadata extension.
        /// </summary>
        /// <param name="location">The location of the dictionary file (<code>*.dict</code>).</param>
        /// <returns>An instantiated dictionary.</returns>
        /// <exception cref="IOException">If an I/O error occurs.</exception>
        public static Dictionary Read(string location)
        {
            string metadata = DictionaryMetadata.GetExpectedMetadataLocation(location);

            using (var fsaStream = File.OpenRead(location))
            using (var metaDataStream = File.OpenRead(metadata))
                return Read(fsaStream, metaDataStream);
        }

        /// <summary>
        /// Attempts to load a dictionary from opened streams of FSA dictionary data
        /// and associated metadata. Input streams are not disposed automatically.
        /// </summary>
        /// <param name="fsaStream">The stream with FSA data.</param>
        /// <param name="metadataStream">The stream with metadata.</param>
        /// <returns>Returns an instantiated <see cref="Dictionary"/>.</returns>
        /// <exception cref="IOException">IOException if an I/O error occurs.</exception>
        public static Dictionary Read(Stream fsaStream, Stream metadataStream)
        {
            return new Dictionary(FSA.Read(fsaStream), DictionaryMetadata.Read(metadataStream));
        }
    }
}
