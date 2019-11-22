using J2N;
using System.IO;

namespace Morfologik.Fsa
{
    /// <summary>
    /// Standard FSA file header, as described in <code>fsa</code> package
    /// documentation.
    /// </summary>
    public sealed class FSAHeader
    {
        /// <summary>
        /// FSA magic (4 bytes).
        /// </summary>
        internal const int FsaMagic =
            ('\\' << 24) |
            ('f' << 16) |
            ('s' << 8) |
            ('a');

        /// <summary>
        /// Maximum length of the header block.
        /// </summary>
        internal const int MaxHeaderLength = 4 + 8;

        /// <summary>FSA version number.</summary>
        internal readonly byte version;

        internal FSAHeader(byte version)
        {
            this.version = version;
        }

        /// <summary>
        /// Read FSA header and version from a stream, consuming read bytes.
        /// </summary>
        /// <param name="input">The input stream to read data from.</param>
        /// <returns>Returns a valid <see cref="FSAHeader"/> with version information.</returns>
        /// <exception cref="IOException">If the stream ends prematurely or if it contains invalid data.</exception>
        public static FSAHeader Read(Stream input)
        {
            if (input.ReadByte() != ((FsaMagic.TripleShift(24))) ||
                input.ReadByte() != ((FsaMagic.TripleShift(16)) & 0xff) ||
                input.ReadByte() != ((FsaMagic.TripleShift(8)) & 0xff) ||
                input.ReadByte() != ((FsaMagic) & 0xff))
            {
                throw new IOException("Invalid file header, probably not an FSA.");
            }

            int version = input.ReadByte();
            if (version == -1)
            {
                throw new IOException("Truncated file, no version number.");
            }

            return new FSAHeader((byte)version);
        }

        /// <summary>
        /// Writes FSA magic bytes and version information.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        /// <param name="version">Automaton version.</param>
        /// <exception cref="IOException">Rethrown if writing fails.</exception>
        public static void Write(Stream output, byte version)
        {
            output.WriteByte((FsaMagic >> 24) & 0xff);
            output.WriteByte((FsaMagic >> 16) & 0xff);
            output.WriteByte((FsaMagic >> 8) & 0xff);
            output.WriteByte((FsaMagic) & 0xff);
            output.WriteByte(version);
        }
    }
}
