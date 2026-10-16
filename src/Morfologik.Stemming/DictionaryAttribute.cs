using J2N.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Attributes applying to <see cref="Dictionary"/> and <see cref="DictionaryMetadata"/>.
    /// </summary>
    public enum DictionaryAttribute
    {
        /// <summary>
        /// Logical fields separator inside the FSA.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="System.Text.Encoding"/>.
        /// </remarks>
        Separator = 0,

        /// <summary>
        /// Character to byte encoding used for strings inside the FSA.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="char"/>.
        /// </remarks>
        Encoding = 1,

        /// <summary>
        /// If the FSA dictionary includes frequency data.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="bool"/>.
        /// </remarks>
        FrequencyIncluded = 2,

        /// <summary>
        /// If the spelling dictionary is supposed to ignore words containing digits.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="bool"/>.
        /// </remarks>
        IgnoreNumbers = 3,

        /// <summary>
        /// If the spelling dictionary is supposed to ignore punctuation.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="bool"/>.
        /// </remarks>
        IgnorePunctuation = 4,

        /// <summary>
        /// If the spelling dictionary is supposed to ignore CamelCase words.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="bool"/>.
        /// </remarks>
        IgnoreCamelCase = 5,

        /// <summary>
        /// If the spelling dictionary is supposed to ignore ALL UPPERCASE words.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="bool"/>.
        /// </remarks>
        IgnoreAllUpperCase = 6,

        /// <summary>
        /// If the spelling dictionary is supposed to ignore diacritics, so that
        /// 'a' would be treated as equivalent to 'ą'.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="bool"/>.
        /// </remarks>
        IgnoreDiacritics = 7,

        /// <summary>
        /// If the spelling dictionary is supposed to treat upper and lower case
        /// as equivalent.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="bool"/>.
        /// </remarks>
        ConvertCase = 8,

        /// <summary>
        /// If the spelling dictionary is supposed to split runOnWords.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="bool"/>.
        /// </remarks>
        RunOnWords = 9,

        /// <summary>
        /// Locale associated with the dictionary.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="CultureInfo"/>.
        /// </remarks>
        Culture = 10,

        /// <summary>
        /// <see cref="EncoderType"/> associated with the dictionary.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="EncoderType"/>.
        /// </remarks>
        Encoder = 11,

        /// <summary>
        /// Input conversion pairs to replace non-standard characters before search in a speller dictionary.
        /// For example, common ligatures can be replaced here.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <c>J2N.Collections.Generic.OrderedDictionary&lt;string, string&gt;</c>.
        /// </remarks>
        InputConversion = 12,

        /// <summary>
        /// Output conversion pairs to replace non-standard characters before search in a speller dictionary.
        /// For example, standard characters can be replaced here into ligatures.
        /// <para/>
        /// Useful for dictionaries that do have certain standards imposed.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <c>J2N.Collections.Generic.OrderedDictionary&lt;string, string&gt;</c>.
        /// </remarks>
        OutputConversion = 13,

        /// <summary>
        /// Replacement pairs for non-obvious candidate search in a speller dictionary.
        /// For example, Polish <c>rz</c> is phonetically equivalent to <c>ż</c>,
        /// and this may be specified here to allow looking for replacements of <c>rz</c> with <c>ż</c>
        /// and vice versa.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <c>J2N.Collections.Generic.OrderedDictionary&lt;string, IList&lt;string&gt;&gt;</c>.
        /// </remarks>
        ReplacementPairs = 14,

        /// <summary>
        /// Equivalent characters (treated similarly as equivalent chars with and without
        /// diacritics). For example, Polish <c>ł</c> can be specified as equivalent to <c>l</c>.
        /// <para/>
        /// This implements a feature similar to hunspell MAP in the affix file.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <c>J2N.Collections.Generic.OrderedDictionary&lt;char, IList&lt;char&gt;&gt;</c>.
        /// </remarks>
        EquivalentChars = 15,

        /// <summary>
        /// Dictionary license attribute.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="string"/>.
        /// </remarks>
        License = 16,

        /// <summary>
        /// Dictionary author.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="string"/>.
        /// </remarks>
        Author = 17,

        /// <summary>
        /// Dictionary creation date.
        /// </summary>
        /// <remarks>
        /// The type for <see cref="DictionaryAttributeExtensions.ConvertValueFromString{TValue}(DictionaryAttribute, string, out TValue)"/>
        /// is <see cref="string"/>.
        /// </remarks>
        CreationDate = 18,
    }

    /// <summary>
    /// Extensions to <see cref="DictionaryAttribute"/>.
    /// </summary>
    public static class DictionaryAttributeExtensions
    {
        private static readonly DictionaryAttributeDescriptors attributeDescriptors = new();

        /// <summary>
        /// Converts a string to the given attribute's value.
        /// </summary>
        /// <param name="attribute">This attribute.</param>
        /// <param name="value">The value to convert to an attribute value.</param>
        /// <returns>Returns the attribute's value converted from a string.</returns>
        /// <exception cref="ArgumentException">If the input string cannot be converted to the attribute's value.</exception>
        public static object FromString(this DictionaryAttribute attribute, string value)
        {
            return attributeDescriptors[attribute].Parse(value);
        }

        /// <summary>
        /// Converts a string to the given attribute's value without boxing.
        /// </summary>
        /// <param name="attribute">This attribute.</param>
        /// <param name="value">The value to convert to an attribute value.</param>
        /// <param name="converted">Upon successful completion of the operation, outputs
        /// the converted <typeparamref name="TValue"/> instance.</param>
        /// <returns>Returns the attribute's value converted from a string.</returns>
        /// <exception cref="ArgumentException">If the input string cannot be converted to the attribute's value.</exception>
        public static void ConvertValueFromString<TValue>(this DictionaryAttribute attribute, string value, out TValue converted)
        {
            attributeDescriptors[attribute].ConvertValueFromString(value, out converted);
        }

        /// <summary>
        /// Returns a <see cref="DictionaryAttribute"/> associated with
        /// a given <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="propertyName">The property of a <see cref="DictionaryAttribute"/>.</param>
        /// <returns>Return a <see cref="DictionaryAttribute"/> associated with
        /// a given <paramref name="propertyName"/>.</returns>
        public static DictionaryAttribute FromPropertyName(string propertyName)
        {
            if (attributeDescriptors.attrsByPropertyName.TryGetValue(propertyName, out DictionaryAttribute value))
            {
                return value;
            }
            throw new ArgumentException($"No attribute for property: {propertyName}");
        }

        /// <summary>
        /// Gets the current <see cref="DictionaryAttribute"/>'s property name.
        /// </summary>
        /// <param name="attribute">This attribute.</param>
        /// <returns>The property name string, i.e. <c>"fsa.dict.license"</c>.</returns>
        public static string GetPropertyName(this DictionaryAttribute attribute)
        {
            return attributeDescriptors[attribute].PropertyName;
        }

        #region Nested Class: DictionaryAttributeDescriptor

        /// <summary>
        /// Base class for <see cref="DictionaryAttributeDescriptor{TValue}"/> instances.
        /// </summary>
        /// <remarks>
        /// This class acts as a bridge to allow properties to be converted without boxing,
        /// while still allowing for a common interface to be used for all attributes.
        /// </remarks>
        internal abstract class DictionaryAttributeDescriptor
        {
            protected DictionaryAttributeDescriptor(string propertyName, DictionaryAttribute attribute)
            {
                PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
                Attribute = attribute;
            }

            /// <summary>
            /// Property name for this attribute.
            /// </summary>
            public string PropertyName { get; }

            /// <summary>
            /// The corresponding <see cref="DictionaryAttribute"/> enum value.
            /// </summary>
            public DictionaryAttribute Attribute { get; }

            /// <summary>
            /// Converts a string to the given attribute's value.
            /// </summary>
            /// <param name="value">The value to convert to an attribute value.</param>
            /// <returns>Returns the attribute's value converted from a string.</returns>
            /// <exception cref="ArgumentException">If the input string cannot be converted to the attribute's value.</exception>
            internal abstract object Parse(string value);

            /// <summary>
            /// Convert the specified string to an instance of TValue using the attribute and assign it to destination.
            /// </summary>
            /// <typeparam name="TValue">The target type produced from the string.</typeparam>
            /// <param name="value">The value to convert to an attribute value.</param>
            /// <param name="converted">Upon successful completion of the operation, outputs
            /// the converted <typeparamref name="TValue"/> instance.</param>
            /// <exception cref="ArgumentException">
            /// The attribute does not produce a value of type <typeparamref name="TValue"/>.
            /// <para/>
            /// -or-
            /// <para/>
            /// The input string cannot be converted to the attribute's value.
            /// </exception>
            public void ConvertValueFromString<TValue>(string value, out TValue converted)
            {
                if (this is not DictionaryAttributeDescriptor<TValue> typedAttribute)
                {
                    throw new ArgumentException(
                        $"Attribute '{PropertyName}' does not produce a value of type " +
                        $"'{typeof(TValue)}'.");
                }

                converted = typedAttribute.FromString(value);
            }
        }

        #endregion Nested Class: DictionaryAttributeDescriptor

        #region Nested Class: DictionaryAttributeDescriptor<TValue>

        /// <summary>
        /// <see cref="DictionaryAttribute"/> instance type.
        /// </summary>
        /// <typeparam name="TValue">The type of return (T)value of <see cref="FromString(string)"/>.</typeparam>
        private class DictionaryAttributeDescriptor<TValue> : DictionaryAttributeDescriptor
        {
            private readonly Func<string, string, TValue>? fromString = null;

            /// <summary>
            /// Public instance constructor.
            /// </summary>
            internal DictionaryAttributeDescriptor(string propertyName, DictionaryAttribute attribute)
                : base(propertyName, attribute)
            {
            }

            /// <summary>
            /// Public instance constructor.
            /// </summary>
            internal DictionaryAttributeDescriptor(string propertyName, DictionaryAttribute attribute, Func<string, string, TValue> fromString)
                : base(propertyName, attribute)
            {
                this.fromString = fromString ?? throw new ArgumentNullException(nameof(fromString));
            }

            /// <summary>
            /// Converts a string to the given attribute's value.
            /// </summary>
            /// <param name="value">The value to convert to an attribute value.</param>
            /// <returns>Returns the attribute's value converted from a string.</returns>
            /// <exception cref="ArgumentException">If the input string cannot be converted to the attribute's value.</exception>
            public TValue FromString(string value)
            {
                if (fromString != null)
                {
                    return fromString(PropertyName, value);
                }

                Debug.Assert(typeof(TValue) == typeof(string));

                return Unsafe.As<string, TValue>(ref value);
            }

            internal override object Parse(string value)
            {
                return FromString(value)!;
            }

        }

        #endregion Nested Class: DictionaryAttributeDescriptor<TValue>

        #region Nested Class: DictionaryAttributeDescriptors


        internal sealed class DictionaryAttributeDescriptors
        {
            /// <summary>
            /// Maps property names to their associated DictionaryAttribute instances for fast lookup.
            /// </summary>
            internal readonly JCG.Dictionary<string, DictionaryAttribute> attrsByPropertyName = new();

            /// <summary>
            /// Mapping for <see cref="DictionaryAttribute"/> to <see cref="DictionaryAttributeDescriptor"/> for
            /// fast lookup.
            /// </summary>
            private readonly DictionaryAttributeDescriptor[] attributeDescriptors;

            public DictionaryAttributeDescriptors()
            {
                DictionaryAttributeDescriptor[] attributeDescriptorArray =
                {
                    // Separator (0)
                    new DictionaryAttributeDescriptor<char>("fsa.dict.separator", DictionaryAttribute.Separator,
                        fromString: (string propertyName, string separator) =>
                        {
                            if (separator == null || separator.Length != 1)
                            {
                                throw new ArgumentException("Attribute " + propertyName
                                    + " must be a single character.");
                            }

                            char charValue = separator[0];
                            if (char.IsHighSurrogate(charValue) ||
                                char.IsLowSurrogate(charValue))
                            {
                                throw new ArgumentException(
                                    "Field separator character cannot be part of a surrogate pair: " + separator);
                            }

                            return charValue;
                        }),

                    // Encoding (1)
                    new DictionaryAttributeDescriptor<Encoding>("fsa.dict.encoding", DictionaryAttribute.Encoding,
                        fromString: (string propertyName, string encodingName) =>
                        {
                            return Encoding.GetEncoding(encodingName, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                        }),

                    // FrequencyIncluded (2)
                    new DictionaryAttributeDescriptor<bool>("fsa.dict.frequency-included", DictionaryAttribute.FrequencyIncluded,
                        fromString: (string propertyName, string value) =>
                        {
                            return BooleanValue(value);
                        }),

                    // IgnoreNumbers (3)
                    new DictionaryAttributeDescriptor<bool>("fsa.dict.speller.ignore-numbers", DictionaryAttribute.IgnoreNumbers,
                        fromString: (string propertyName, string value) =>
                        {
                            return BooleanValue(value);
                        }),

                    // IgnorePunctuation (4)
                    new DictionaryAttributeDescriptor<bool>("fsa.dict.speller.ignore-punctuation", DictionaryAttribute.IgnorePunctuation,
                        fromString: (string propertyName, string value) =>
                        {
                            return BooleanValue(value);
                        }),

                    // IgnoreCamelCase (5)
                    new DictionaryAttributeDescriptor<bool>("fsa.dict.speller.ignore-camel-case", DictionaryAttribute.IgnoreCamelCase,
                        fromString: (string propertyName, string value) =>
                        {
                            return BooleanValue(value);
                        }),

                    // IgnoreAllUpperCase (6)
                    new DictionaryAttributeDescriptor<bool>("fsa.dict.speller.ignore-all-uppercase", DictionaryAttribute.IgnoreAllUpperCase,
                    fromString: (string propertyName, string value) =>
                        {
                            return BooleanValue(value);
                        }),

                    // IgnoreDiacritics (7)
                    new DictionaryAttributeDescriptor<bool>("fsa.dict.speller.ignore-diacritics", DictionaryAttribute.IgnoreDiacritics,
                        fromString: (string propertyName, string value) =>
                        {
                            return BooleanValue(value);
                        }),

                    // ConvertCase (8)
                    new DictionaryAttributeDescriptor<bool>("fsa.dict.speller.convert-case", DictionaryAttribute.ConvertCase,
                        fromString: (string propertyName, string value) =>
                        {
                            return BooleanValue(value);
                        }),

                    // RunOnWords (9)
                    new DictionaryAttributeDescriptor<bool>("fsa.dict.speller.runon-words", DictionaryAttribute.RunOnWords,
                        fromString: (string propertyName, string value) =>
                        {
                            return BooleanValue(value);
                        }),

                    // Culture (10)
                    new DictionaryAttributeDescriptor<CultureInfo>("fsa.dict.speller.locale", DictionaryAttribute.Culture,
                        fromString: (string propertyName, string value) =>
                        {
                            return new CultureInfo(value); // TODO: Convert from Java locale format
                        }),

                    // Encoder (11)
                    new DictionaryAttributeDescriptor<EncoderType>("fsa.dict.encoder", DictionaryAttribute.Encoder,
                        fromString: (string propertyName, string value) =>
                        {
                            try
                            {
                                return (EncoderType)Enum.Parse(typeof(EncoderType), value.Trim(), true);
                            }
                            catch (ArgumentException e)
                            {
                                throw new ArgumentException($"Invalid encoder name '{value.Trim()}', only these coders are valid: {Enum.GetValues(typeof(EncoderType))}", e);
                            }
                        }),

                    // InputConversion (12)
                    new DictionaryAttributeDescriptor<JCG.OrderedDictionary<string, string>>("fsa.dict.input-conversion", DictionaryAttribute.InputConversion,
                        fromString: (string propertyName, string value) =>
                        {
                            var conversionPairs = new JCG.OrderedDictionary<string, string>();
                            string[] replacements = PairSplit!.Split(value);
                            foreach (var stringPair in replacements)
                            {
                                var twoStrings = stringPair.Trim().Split(' ');
                                if (twoStrings.Length == 2)
                                {
                                    if (!conversionPairs.TryAdd(twoStrings[0], twoStrings[1]))
                                        throw new ArgumentException($"Input conversion cannot specify different values for the same input string: {twoStrings[0]}");
                                }
                                else
                                {
                                    throw new ArgumentException($"Attribute {propertyName}  is not in the proper format: {value}");
                                }
                            }
                            return conversionPairs;
                        }),

                    // OutputConversion (13)
                    new DictionaryAttributeDescriptor<JCG.OrderedDictionary<string, string>>("fsa.dict.output-conversion", DictionaryAttribute.OutputConversion,
                        fromString: (string propertyName, string value) =>
                        {
                            var conversionPairs = new JCG.OrderedDictionary<string, string>();
                            string[] replacements = PairSplit!.Split(value);
                            foreach (var stringPair in replacements)
                            {
                                var twoStrings = stringPair.Trim().Split(' ');
                                if (twoStrings.Length == 2)
                                {
                                    if (!conversionPairs.TryAdd(twoStrings[0], twoStrings[1]))
                                        throw new ArgumentException($"Output conversion cannot specify different values for the same input string: {twoStrings[0]}");
                                }
                                else
                                {
                                    throw new ArgumentException($"Attribute {propertyName}  is not in the proper format: {value}");
                                }
                            }
                            return conversionPairs;
                        }),

                    // ReplacementPairs (14)
                    new DictionaryAttributeDescriptor<JCG.OrderedDictionary<string, IList<string>>>("fsa.dict.speller.replacement-pairs", DictionaryAttribute.ReplacementPairs,
                        fromString: (string propertyName, string value) =>
                        {
                            var replacementPairs = new JCG.OrderedDictionary<string, IList<string>>();
                            string[] replacements = PairSplit!.Split(value);
                            foreach (var stringPair in replacements)
                            {
                                var twoStrings = stringPair.Trim().Split(' ');
                                if (twoStrings.Length == 2)
                                {
                                    ref IList<string>? strList = ref CollectionMarshal.GetValueRefOrAddDefault(replacementPairs, twoStrings[0], out bool exists);

                                    // Set the value to a new list if it doesn't exist yet
                                    if (!exists)
                                        strList = new List<string>();

                                    strList!.Add(twoStrings[1]);
                                }
                                else
                                {
                                    throw new ArgumentException($"Attribute {propertyName}  is not in the proper format: {value}");
                                }
                            }
                            return replacementPairs;
                        }),

                    // EquivalentChars (15)
                    new DictionaryAttributeDescriptor<JCG.OrderedDictionary<char, IList<char>>>("fsa.dict.speller.equivalent-chars", DictionaryAttribute.EquivalentChars,
                        fromString: (string propertyName, string value) =>
                        {
                            var equivalentCharacters = new JCG.OrderedDictionary<char, IList<char>>();
                            string[] eqChars = PairSplit!.Split(value);
                            foreach (var characterPair in eqChars)
                            {
                                var twoChars = characterPair.Trim().Split(' ');
                                if (twoChars.Length == 2
                                    && twoChars[0].Length == 1
                                    && twoChars[1].Length == 1)
                                {
                                    char fromChar = twoChars[0][0];
                                    char toChar = twoChars[1][0];

                                    ref IList<char>? chList = ref CollectionMarshal.GetValueRefOrAddDefault(equivalentCharacters, fromChar, out bool exists);

                                    // Set the value to a new list if it doesn't exist yet
                                    if (!exists)
                                        chList = new List<char>();

                                    chList!.Add(toChar);
                                }
                                else
                                {
                                    throw new ArgumentException($"Attribute {propertyName}  is not in the proper format: {value}");
                                }
                            }
                            return equivalentCharacters;
                        }),

                    // License (16)
                    new DictionaryAttributeDescriptor<string>("fsa.dict.license", DictionaryAttribute.License),

                    // Author (17)
                    new DictionaryAttributeDescriptor<string>("fsa.dict.author", DictionaryAttribute.Author),

                    // CreationDate (18)
                    new DictionaryAttributeDescriptor<string>("fsa.dict.created", DictionaryAttribute.CreationDate),
                };

                attributeDescriptors = attributeDescriptorArray;

                foreach (DictionaryAttributeDescriptor attributeDescriptor in attributeDescriptorArray)
                {
                    attrsByPropertyName[attributeDescriptor.PropertyName] = attributeDescriptor.Attribute;
                }
            }

            public DictionaryAttributeDescriptor this[DictionaryAttribute attribute]
            {
                get
                {
                    int index = (int)attribute;
                    if ((uint)index < (uint)attributeDescriptors.Length &&
                        attributeDescriptors[index] is DictionaryAttributeDescriptor field)
                    {
                        return field;
                    }
                    throw new ArgumentException($"No attribute for property: {nameof(attribute)}");
                }
            }

            private static readonly Regex PairSplit = new Regex(",\\s*", RegexOptions.Compiled);

            private static bool BooleanValue(string value)
            {
                if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                throw new ArgumentException($"Not a boolean value: {value}");
            }
        }

        #endregion Nested Class: DictionaryAttributeDescriptors
    }
}
