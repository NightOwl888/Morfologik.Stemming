using System;
using System.Collections.Generic;
using System.Globalization;
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
        Separator,

        /// <summary>
        /// Character to byte encoding used for strings inside the FSA.
        /// </summary>
        Encoding,

        /// <summary>
        /// If the FSA dictionary includes frequency data.
        /// </summary>
        FrequencyIncluded,
        /// <summary>
        /// If the spelling dictionary is supposed to ignore words containing digits.
        /// </summary>
        IgnoreNumbers,

        /// <summary>
        /// If the spelling dictionary is supposed to ignore punctuation.
        /// </summary>
        IgnorePunctuation,

        /// <summary>
        /// If the spelling dictionary is supposed to ignore CamelCase words.
        /// </summary>
        IgnoreCamelCase,

        /// <summary>
        /// If the spelling dictionary is supposed to ignore ALL UPPERCASE words.
        /// </summary>
        IgnoreAllUpperCase,

        /// <summary>
        /// If the spelling dictionary is supposed to ignore diacritics, so that
        /// 'a' would be treated as equivalent to 'ą'.
        /// </summary>
        IgnoreDiacritics,

        /// <summary>
        /// If the spelling dictionary is supposed to treat upper and lower case
        /// as equivalent.
        /// </summary>
        ConvertCase,

        /// <summary>
        /// If the spelling dictionary is supposed to split runOnWords.
        /// </summary>
        RunOnWords,

        /// <summary>
        /// Locale associated with the dictionary.
        /// </summary>
        Culture,

        /// <summary>
        /// <see cref="EncoderType"/> associated with the dictionary.
        /// </summary>
        Encoder,

        /// <summary>
        /// Input conversion pairs to replace non-standard characters before search in a speller dictionary.
        /// For example, common ligatures can be replaced here.
        /// </summary>
        InputConversion,

        /// <summary>
        /// Output conversion pairs to replace non-standard characters before search in a speller dictionary.
        /// For example, standard characters can be replaced here into ligatures.
        /// <para/>
        /// Useful for dictionaries that do have certain standards imposed.
        /// </summary>
        OutputConversion,

        /// <summary>
        /// Replacement pairs for non-obvious candidate search in a speller dictionary.
        /// For example, Polish <c>rz</c> is phonetically equivalent to <c>ż</c>,
        /// and this may be specified here to allow looking for replacements of <c>rz</c> with <c>ż</c>
        /// and vice versa.
        /// </summary>
        ReplacementPairs,

        /// <summary>
        /// Equivalent characters (treated similarly as equivalent chars with and without
        /// diacritics). For example, Polish <c>ł</c> can be specified as equivalent to <c>l</c>.
        /// <para/>
        /// This implements a feature similar to hunspell MAP in the affix file.
        /// </summary>
        EquivalentChars,

        /// <summary>
        /// Dictionary license attribute.
        /// </summary>
        License,

        /// <summary>
        /// Dictionary author.
        /// </summary>
        Author,

        /// <summary>
        /// Dictionary creation date.
        /// </summary>
        CreationDate,
    }

    /// <summary>
    /// Extensions to <see cref="DictionaryAttribute"/>.
    /// </summary>
    public static class DictionaryAttributeExtensions
    {
        /// <summary>
        /// Converts a string to the given attribute's value.
        /// </summary>
        /// <param name="attribute">This attribute.</param>
        /// <param name="value">The value to convert to an attribute value.</param>
        /// <returns>Returns the attribute's value converted from a string.</returns>
        /// <exception cref="ArgumentException">If the input string cannot be converted to the attribute's value.</exception>
        public static object FromString(this DictionaryAttribute attribute, string value)
        {
            switch (attribute)
            {
                case DictionaryAttribute.Separator:             return Separator.FromString(value);
                case DictionaryAttribute.Encoding:              return Encoding.FromString(value);
                case DictionaryAttribute.FrequencyIncluded:     return FrequencyIncluded.FromString(value);
                case DictionaryAttribute.IgnoreNumbers:         return IgnoreNumbers.FromString(value);
                case DictionaryAttribute.IgnorePunctuation:     return IgnorePunctuation.FromString(value);
                case DictionaryAttribute.IgnoreCamelCase:       return IgnoreCamelCase.FromString(value);
                case DictionaryAttribute.IgnoreAllUpperCase:    return IgnoreAllUpperCase.FromString(value);
                case DictionaryAttribute.IgnoreDiacritics:      return IgnoreDiacritics.FromString(value);
                case DictionaryAttribute.ConvertCase:           return ConvertCase.FromString(value);
                case DictionaryAttribute.RunOnWords:            return RunOnWords.FromString(value);
                case DictionaryAttribute.Culture:               return Culture.FromString(value);
                case DictionaryAttribute.Encoder:               return Encoder.FromString(value);
                case DictionaryAttribute.InputConversion:       return InputConversion.FromString(value);
                case DictionaryAttribute.OutputConversion:      return OutputConversion.FromString(value);
                case DictionaryAttribute.ReplacementPairs:      return ReplacementPairs.FromString(value);
                case DictionaryAttribute.EquivalentChars:       return EquivalentChars.FromString(value);
                case DictionaryAttribute.License:               return License.FromString(value);
                case DictionaryAttribute.Author:                return Author.FromString(value);
                case DictionaryAttribute.CreationDate:          return CreationDate.FromString(value);
            }
            throw new ArgumentException($"No attribute for property: {nameof(attribute)}");
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
            switch (propertyName)
            {
                case "fsa.dict.separator":                      return DictionaryAttribute.Separator;
                case "fsa.dict.encoding":                       return DictionaryAttribute.Encoding;
                case "fsa.dict.frequency-included":             return DictionaryAttribute.FrequencyIncluded;
                case "fsa.dict.speller.ignore-numbers":         return DictionaryAttribute.IgnoreNumbers;
                case "fsa.dict.speller.ignore-punctuation":     return DictionaryAttribute.IgnorePunctuation;
                case "fsa.dict.speller.ignore-camel-case":      return DictionaryAttribute.IgnoreCamelCase;
                case "fsa.dict.speller.ignore-all-uppercase":   return DictionaryAttribute.IgnoreAllUpperCase;
                case "fsa.dict.speller.ignore-diacritics":      return DictionaryAttribute.IgnoreDiacritics;
                case "fsa.dict.speller.convert-case":           return DictionaryAttribute.ConvertCase;
                case "fsa.dict.speller.runon-words":            return DictionaryAttribute.RunOnWords;
                case "fsa.dict.speller.locale":                 return DictionaryAttribute.Culture;
                case "fsa.dict.encoder":                        return DictionaryAttribute.Encoder;
                case "fsa.dict.input-conversion":               return DictionaryAttribute.InputConversion;
                case "fsa.dict.output-conversion":              return DictionaryAttribute.OutputConversion;
                case "fsa.dict.speller.replacement-pairs":      return DictionaryAttribute.ReplacementPairs;
                case "fsa.dict.speller.equivalent-chars":       return DictionaryAttribute.EquivalentChars;
                case "fsa.dict.license":                        return DictionaryAttribute.License;
                case "fsa.dict.author":                         return DictionaryAttribute.Author;
                case "fsa.dict.created":                        return DictionaryAttribute.CreationDate;
            }
            throw new ArgumentException("No attribute for property: " + propertyName);
        }

        /// <summary>
        /// Gets the current <see cref="DictionaryAttribute"/>'s property name.
        /// </summary>
        /// <param name="attribute">This attribute.</param>
        /// <returns>The property name string, i.e. <c>"fsa.dict.license"</c>.</returns>
        public static string GetPropertyName(this DictionaryAttribute attribute)
        {
            switch (attribute)
            {
                case DictionaryAttribute.Separator:             return Separator.PropertyName;
                case DictionaryAttribute.Encoding:              return Encoding.PropertyName;
                case DictionaryAttribute.FrequencyIncluded:     return FrequencyIncluded.PropertyName;
                case DictionaryAttribute.IgnoreNumbers:         return IgnoreNumbers.PropertyName;
                case DictionaryAttribute.IgnorePunctuation:     return IgnorePunctuation.PropertyName;
                case DictionaryAttribute.IgnoreCamelCase:       return IgnoreCamelCase.PropertyName;
                case DictionaryAttribute.IgnoreAllUpperCase:    return IgnoreAllUpperCase.PropertyName;
                case DictionaryAttribute.IgnoreDiacritics:      return IgnoreDiacritics.PropertyName;
                case DictionaryAttribute.ConvertCase:           return ConvertCase.PropertyName;
                case DictionaryAttribute.RunOnWords:            return RunOnWords.PropertyName;
                case DictionaryAttribute.Culture:               return Culture.PropertyName;
                case DictionaryAttribute.Encoder:               return Encoder.PropertyName;
                case DictionaryAttribute.InputConversion:       return InputConversion.PropertyName;
                case DictionaryAttribute.OutputConversion:      return OutputConversion.PropertyName;
                case DictionaryAttribute.ReplacementPairs:      return ReplacementPairs.PropertyName;
                case DictionaryAttribute.EquivalentChars:       return EquivalentChars.PropertyName;
                case DictionaryAttribute.License:               return License.PropertyName;
                case DictionaryAttribute.Author:                return Author.PropertyName;
                case DictionaryAttribute.CreationDate:          return CreationDate.PropertyName;
            }
            throw new ArgumentException($"No attribute for property: {nameof(attribute)}");
        }

        #region Attribute Fields

        /// <summary>
        /// Logical fields separator inside the FSA.
        /// </summary>
        public static readonly DictionaryAttribute<char> Separator = new DictionaryAttribute<char>("fsa.dict.separator", DictionaryAttribute.Separator,
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
            });

        /// <summary>
        /// Character to byte encoding used for strings inside the FSA.
        /// </summary>
        public static readonly DictionaryAttribute<Encoding> Encoding = new DictionaryAttribute<Encoding>("fsa.dict.encoding", DictionaryAttribute.Encoding,
            fromString: (string propertyName, string encodingName) =>
            {
                return System.Text.Encoding.GetEncoding(encodingName);
            });

        /// <summary>
        /// If the FSA dictionary includes frequency data.
        /// </summary>
        public static readonly DictionaryAttribute<bool> FrequencyIncluded = new DictionaryAttribute<bool>("fsa.dict.frequency-included", DictionaryAttribute.FrequencyIncluded,
            fromString: (string propertyName, string value) =>
            {
                return BooleanValue(value);
            });

        /// <summary>
        /// If the spelling dictionary is supposed to ignore words containing digits.
        /// </summary>
        public static readonly DictionaryAttribute<bool> IgnoreNumbers = new DictionaryAttribute<bool>("fsa.dict.speller.ignore-numbers", DictionaryAttribute.IgnoreNumbers,
            fromString: (string propertyName, string value) =>
            {
                return BooleanValue(value);
            });

        /// <summary>
        /// If the spelling dictionary is supposed to ignore punctuation.
        /// </summary>
        public static readonly DictionaryAttribute<bool> IgnorePunctuation = new DictionaryAttribute<bool>("fsa.dict.speller.ignore-punctuation", DictionaryAttribute.IgnorePunctuation,
            fromString: (string propertyName, string value) =>
            {
                return BooleanValue(value);
            });

        /// <summary>
        /// If the spelling dictionary is supposed to ignore CamelCase words.
        /// </summary>
        public static readonly DictionaryAttribute<bool> IgnoreCamelCase = new DictionaryAttribute<bool>("fsa.dict.speller.ignore-camel-case", DictionaryAttribute.IgnoreCamelCase,
            fromString: (string propertyName, string value) =>
            {
                return BooleanValue(value);
            });

        /// <summary>
        /// If the spelling dictionary is supposed to ignore ALL UPPERCASE words.
        /// </summary>
        public static readonly DictionaryAttribute<bool> IgnoreAllUpperCase = new DictionaryAttribute<bool>("fsa.dict.speller.ignore-all-uppercase", DictionaryAttribute.IgnoreAllUpperCase,
            fromString: (string propertyName, string value) =>
            {
                return BooleanValue(value);
            });

        /// <summary>
        /// If the spelling dictionary is supposed to ignore diacritics, so that
        /// 'a' would be treated as equivalent to 'ą'.
        /// </summary>
        public static readonly DictionaryAttribute<bool> IgnoreDiacritics = new DictionaryAttribute<bool>("fsa.dict.speller.ignore-diacritics", DictionaryAttribute.IgnoreDiacritics,
            fromString: (string propertyName, string value) =>
            {
                return BooleanValue(value);
            });

        /// <summary>
        /// If the spelling dictionary is supposed to treat upper and lower case
        /// as equivalent.
        /// </summary>
        public static readonly DictionaryAttribute<bool> ConvertCase = new DictionaryAttribute<bool>("fsa.dict.speller.convert-case", DictionaryAttribute.ConvertCase,
            fromString: (string propertyName, string value) =>
            {
                return BooleanValue(value);
            });

        /// <summary>
        /// If the spelling dictionary is supposed to split runOnWords.
        /// </summary>
        public static readonly DictionaryAttribute<bool> RunOnWords = new DictionaryAttribute<bool>("fsa.dict.speller.runon-words", DictionaryAttribute.RunOnWords,
            fromString: (string propertyName, string value) =>
            {
                return BooleanValue(value);
            });

        /// <summary>
        /// Locale associated with the dictionary.
        /// </summary>
        public static readonly DictionaryAttribute<CultureInfo> Culture = new DictionaryAttribute<CultureInfo>("fsa.dict.speller.locale", DictionaryAttribute.Culture,
            fromString: (string propertyName, string value) =>
            {
                return new CultureInfo(value); // TODO: Convert from Java locale format
            });

        /// <summary>
        /// <see cref="EncoderType"/> associated with the dictionary.
        /// </summary>
        public static readonly DictionaryAttribute<EncoderType> Encoder = new DictionaryAttribute<EncoderType>("fsa.dict.encoder", DictionaryAttribute.Encoder,
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
            });

        /// <summary>
        /// Input conversion pairs to replace non-standard characters before search in a speller dictionary.
        /// For example, common ligatures can be replaced here.
        /// </summary>
        public static readonly DictionaryAttribute<JCG.LinkedDictionary<string, string>> InputConversion =
            new DictionaryAttribute<JCG.LinkedDictionary<string, string>>("fsa.dict.input-conversion", DictionaryAttribute.InputConversion,
            fromString: (string propertyName, string value) =>
            {
                var conversionPairs = new JCG.LinkedDictionary<string, string>();
                string[] replacements = PairSplit.Split(value);
                foreach (var stringPair in replacements)
                {
                    var twoStrings = stringPair.Trim().Split(' ');
                    if (twoStrings.Length == 2)
                    {
                        if (!conversionPairs.ContainsKey(twoStrings[0]))
                            conversionPairs[twoStrings[0]] = twoStrings[1];
                        else
                            throw new ArgumentException($"Input conversion cannot specify different values for the same input string: {twoStrings[0]}");
                    }
                    else
                    {
                        throw new ArgumentException($"Attribute {propertyName}  is not in the proper format: {value}");
                    }
                }
                return conversionPairs;
            });

        /// <summary>
        /// Output conversion pairs to replace non-standard characters before search in a speller dictionary.
        /// For example, standard characters can be replaced here into ligatures.
        /// <para/>
        /// Useful for dictionaries that do have certain standards imposed.
        /// </summary>
        public static readonly DictionaryAttribute<JCG.LinkedDictionary<string, string>> OutputConversion =
            new DictionaryAttribute<JCG.LinkedDictionary<string, string>>("fsa.dict.output-conversion", DictionaryAttribute.OutputConversion,
            fromString: (string propertyName, string value) =>
            {
                var conversionPairs = new JCG.LinkedDictionary<string, string>();
                string[] replacements = PairSplit.Split(value);
                foreach (var stringPair in replacements)
                {
                    var twoStrings = stringPair.Trim().Split(' ');
                    if (twoStrings.Length == 2)
                    {
                        if (!conversionPairs.ContainsKey(twoStrings[0]))
                            conversionPairs[twoStrings[0]] = twoStrings[1];
                        else
                            throw new ArgumentException($"Output conversion cannot specify different values for the same input string: {twoStrings[0]}");
                    }
                    else
                    {
                        throw new ArgumentException($"Attribute {propertyName}  is not in the proper format: {value}");
                    }
                }
                return conversionPairs;
            });

        /// <summary>
        /// Replacement pairs for non-obvious candidate search in a speller dictionary.
        /// For example, Polish <c>rz</c> is phonetically equivalent to <c>ż</c>,
        /// and this may be specified here to allow looking for replacements of <c>rz</c> with <c>ż</c>
        /// and vice versa.
        /// </summary>
        public static readonly DictionaryAttribute<JCG.LinkedDictionary<string, IList<string>>> ReplacementPairs =
            new DictionaryAttribute<JCG.LinkedDictionary<string, IList<string>>>("fsa.dict.speller.replacement-pairs", DictionaryAttribute.ReplacementPairs,
            fromString: (string propertyName, string value) =>
            {
                var replacementPairs = new JCG.LinkedDictionary<string, IList<string>>();
                string[] replacements = PairSplit.Split(value);
                foreach (var stringPair in replacements)
                {
                    var twoStrings = stringPair.Trim().Split(' ');
                    if (twoStrings.Length == 2)
                    {
                        if (!replacementPairs.ContainsKey(twoStrings[0]))
                            replacementPairs[twoStrings[0]] = new List<string> { twoStrings[1] };
                        else
                            replacementPairs[twoStrings[0]].Add(twoStrings[1]);
                    }
                    else
                    {
                        throw new ArgumentException($"Attribute {propertyName}  is not in the proper format: {value}");
                    }
                }
                return replacementPairs;
            });

        /// <summary>
        /// Equivalent characters (treated similarly as equivalent chars with and without
        /// diacritics). For example, Polish <c>ł</c> can be specified as equivalent to <c>l</c>.
        /// <para/>
        /// This implements a feature similar to hunspell MAP in the affix file.
        /// </summary>
        public static readonly DictionaryAttribute<Dictionary<char, IList<char>>> EquivalentChars =
            new DictionaryAttribute<Dictionary<char, IList<char>>>("fsa.dict.speller.equivalent-chars", DictionaryAttribute.EquivalentChars,
            fromString: (string propertyName, string value) =>
            {
                var equivalentCharacters = new Dictionary<char, IList<char>>();
                string[] eqChars = PairSplit.Split(value);
                foreach (var characterPair in eqChars)
                {
                    var twoChars = characterPair.Trim().Split(' ');
                    if (twoChars.Length == 2
                        && twoChars[0].Length == 1
                        && twoChars[1].Length == 1)
                    {
                        char fromChar = twoChars[0][0];
                        char toChar = twoChars[1][0];
                        if (!equivalentCharacters.ContainsKey(fromChar))
                        {
                            IList<char> chList = new List<char>();
                            equivalentCharacters[fromChar] = chList;
                        }
                        equivalentCharacters[fromChar].Add(toChar);
                    }
                    else
                    {
                        throw new ArgumentException($"Attribute {propertyName}  is not in the proper format: {value}");
                    }
                }
                return equivalentCharacters;
            });

        /// <summary>
        /// Dictionary license attribute.
        /// </summary>
        public static readonly DictionaryAttribute<string> License = new DictionaryAttribute<string>("fsa.dict.license", DictionaryAttribute.License);

        /// <summary>
        /// Dictionary author.
        /// </summary>
        public static readonly DictionaryAttribute<string> Author = new DictionaryAttribute<string>("fsa.dict.author", DictionaryAttribute.Author);

        /// <summary>
        /// Dictionary creation date.
        /// </summary>
        public static readonly DictionaryAttribute<string> CreationDate = new DictionaryAttribute<string>("fsa.dict.created", DictionaryAttribute.CreationDate);


        private static readonly Regex PairSplit = new Regex(",\\s*", RegexOptions.Compiled);

        private static bool BooleanValue(string value)
        {
            value = value.ToLowerInvariant();
            if ("true".Equals(value) || "yes".Equals(value) || "on".Equals(value))
            {
                return true;
            }
            if ("false".Equals(value) || "no".Equals(value) || "off".Equals(value))
            {
                return false;
            }
            throw new ArgumentException("Not a boolean value: " + value);
        }

        #endregion
    }


    /// <summary>
    /// <see cref="DictionaryAttribute"/> instance type.
    /// </summary>
    /// <typeparam name="TValue">The type of return (T)value of <see cref="FromString(string)"/>.</typeparam>
    public class DictionaryAttribute<TValue>
    {
        private readonly Func<string, string, TValue> fromString = null;

        /// <summary>
        /// Public instance constructor.
        /// </summary>
        public DictionaryAttribute(string propertyName, DictionaryAttribute attribute)
        {
            this.PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            this.Attribute = attribute;
        }

        /// <summary>
        /// Public instance constructor.
        /// </summary>
        public DictionaryAttribute(string propertyName, DictionaryAttribute attribute, Func<string, string, TValue> fromString)
        {
            this.PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            this.Attribute = attribute;
            this.fromString = fromString ?? throw new ArgumentNullException(nameof(fromString));
        }

        /// <summary>
        /// Property name for this attribute.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// The corresponding <see cref="Stemming.DictionaryAttribute"/> enum value.
        /// </summary>
        public DictionaryAttribute Attribute { get; }

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

            return (TValue)(object)value;
        }
    }
}
