namespace Morfologik.Stemming
{
    /// <summary>
    /// Known <see cref="ISequenceEncoder"/>s.
    /// </summary>
    public enum EncoderType
    {
        /// <summary>
        /// Corresponds to <see cref="NoEncoder"/>.
        /// </summary>
        None = 0, // Set to 0 to ensure it is the default.

        /// <summary>
        /// Corresponds to <see cref="TrimSuffixEncoder"/>.
        /// </summary>
        Suffix,

        /// <summary>
        /// Corresponds to <see cref="TrimPrefixAndSuffixEncoder"/>.
        /// </summary>
        Prefix,

        /// <summary>
        /// Corresponds to <see cref="TrimInfixAndSuffixEncoder"/>.
        /// </summary>
        Infix,
    }

    /// <summary>
    /// Extensions for <see cref="EncoderType"/>.
    /// </summary>
    public static class EncoderTypeExtensions
    {
        /// <summary>
        /// Gets a new <see cref="ISequenceEncoder"/> instance of the <paramref name="encoderType"/>.
        /// </summary>
        /// <param name="encoderType">This <see cref="EncoderType"/>.</param>
        /// <returns>A new <see cref="ISequenceEncoder"/> instance of the <paramref name="encoderType"/>.</returns>
        public static ISequenceEncoder Get(this EncoderType encoderType)
        {
            switch(encoderType)
            {
                case EncoderType.Suffix:
                    return new TrimSuffixEncoder();
                case EncoderType.Prefix:
                    return new TrimPrefixAndSuffixEncoder();
                case EncoderType.Infix:
                    return new TrimInfixAndSuffixEncoder();
                default:
                    return new NoEncoder();
            }
        }
    }
}
