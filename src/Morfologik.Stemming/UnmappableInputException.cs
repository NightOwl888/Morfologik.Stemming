using System;
using System.ComponentModel;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Thrown when some input cannot be mapped using the declared charset (bytes
    /// to characters or the other way around).
    /// </summary>
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public sealed class UnmappableInputException : Exception
    {
        internal UnmappableInputException(string message, Exception innerException)
            : base(message, innerException)
        { }

        internal UnmappableInputException()
        { }

        internal UnmappableInputException(string message) : base(message)
        { }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        private UnmappableInputException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
