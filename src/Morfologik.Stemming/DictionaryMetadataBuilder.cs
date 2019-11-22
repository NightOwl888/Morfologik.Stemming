using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Helper class to build <see cref="DictionaryMetadata"/> instances.
    /// </summary>
    public sealed class DictionaryMetadataBuilder
    {
        private readonly IDictionary<DictionaryAttribute, string> attrs =
            new Dictionary<DictionaryAttribute, string>();

        public DictionaryMetadataBuilder Separator(char c)
        {
            this.attrs[DictionaryAttribute.Separator] = char.ToString(c);
            return this;
        }

        public DictionaryMetadataBuilder Encoding(Encoding charset)
        {
            return Encoding(charset.WebName);
        }

        public DictionaryMetadataBuilder Encoding(string charsetName)
        {
            this.attrs[DictionaryAttribute.Encoding] = charsetName;
            return this;
        }

        public DictionaryMetadataBuilder FrequencyIncluded() { return FrequencyIncluded(true); }
        public DictionaryMetadataBuilder FrequencyIncluded(bool v) { this.attrs[DictionaryAttribute.FrequencyIncluded] = v.ToString(); return this; }

        public DictionaryMetadataBuilder IgnorePunctuation() { return IgnorePunctuation(true); }
        public DictionaryMetadataBuilder IgnorePunctuation(bool v) { this.attrs[DictionaryAttribute.IgnorePunctuation] = v.ToString(); return this; }

        public DictionaryMetadataBuilder IgnoreNumbers() { return IgnoreNumbers(true); }
        public DictionaryMetadataBuilder IgnoreNumbers(bool v) { this.attrs[DictionaryAttribute.IgnoreNumbers] = v.ToString(); return this; }

        public DictionaryMetadataBuilder IgnoreCamelCase() { return IgnoreCamelCase(true); }
        public DictionaryMetadataBuilder IgnoreCamelCase(bool v) { this.attrs[DictionaryAttribute.IgnoreCamelCase] = v.ToString(); return this; }

        public DictionaryMetadataBuilder IgnoreAllUppercase() { return IgnoreAllUppercase(true); }
        public DictionaryMetadataBuilder IgnoreAllUppercase(bool v) { this.attrs[DictionaryAttribute.IgnoreAllUpperCase] = v.ToString(); return this; }

        public DictionaryMetadataBuilder IgnoreDiacritics() { return IgnoreDiacritics(true); }
        public DictionaryMetadataBuilder IgnoreDiacritics(bool v) { this.attrs[DictionaryAttribute.IgnoreDiacritics] = v.ToString(); return this; }

        public DictionaryMetadataBuilder ConvertCase() { return ConvertCase(true); }
        public DictionaryMetadataBuilder ConvertCase(bool v) { this.attrs[DictionaryAttribute.ConvertCase] = v.ToString(); return this; }

        public DictionaryMetadataBuilder SupportRunOnWords() { return SupportRunOnWords(true); }
        public DictionaryMetadataBuilder SupportRunOnWords(bool v) { this.attrs[DictionaryAttribute.RunOnWords] = v.ToString(); return this; }

        public DictionaryMetadataBuilder Encoder(EncoderType type)
        {
            this.attrs[DictionaryAttribute.Encoder] = type.ToString();
            return this;
        }

        public DictionaryMetadataBuilder Culture(CultureInfo locale)
        {
            return Culture(locale.NativeName);
        }

        public DictionaryMetadataBuilder Culture(string localeName)
        {
            this.attrs[DictionaryAttribute.Culture] = localeName;
            return this;
        }

        public DictionaryMetadataBuilder WithReplacementPairs(IDictionary<string, IList<string>> replacementPairs)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var e in replacementPairs)
            {
                string k = e.Key;
                foreach (string v in e.Value)
                {
                    if (builder.Length > 0) builder.Append(", ");
                    builder.Append(k).Append(" ").Append(v);
                }
            }
            this.attrs[DictionaryAttribute.ReplacementPairs] = builder.ToString();
            return this;
        }

        public DictionaryMetadataBuilder WithEquivalentChars(IDictionary<char, IList<char>> equivalentChars)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var e in equivalentChars)
            {
                char k = e.Key;
                foreach (char v in e.Value)
                {
                    if (builder.Length > 0) builder.Append(", ");
                    builder.Append(k).Append(" ").Append(v);
                }
            }
            this.attrs[DictionaryAttribute.EquivalentChars] = builder.ToString();
            return this;
        }

        public DictionaryMetadataBuilder WithInputConversionPairs(IDictionary<string, string> conversionPairs)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var e in conversionPairs)
            {
                string k = e.Key;
                if (builder.Length > 0) builder.Append(", ");
                builder.Append(k).Append(" ").Append(conversionPairs[k]);
            }
            this.attrs[DictionaryAttribute.InputConversion] = builder.ToString();
            return this;
        }

        public DictionaryMetadataBuilder WithOutputConversionPairs(IDictionary<string, string> conversionPairs)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var e in conversionPairs)
            {
                string k = e.Key;
                if (builder.Length > 0) builder.Append(", ");
                builder.Append(k).Append(" ").Append(conversionPairs[k]);
            }
            this.attrs[DictionaryAttribute.OutputConversion] = builder.ToString();
            return this;
        }


        public DictionaryMetadataBuilder Author(string author)
        {
            this.attrs[DictionaryAttribute.Author] = author;
            return this;
        }

        public DictionaryMetadataBuilder CreationDate(string creationDate)
        {
            this.attrs[DictionaryAttribute.CreationDate] = creationDate;
            return this;
        }

        public DictionaryMetadataBuilder License(string license)
        {
            this.attrs[DictionaryAttribute.License] = license;
            return this;
        }

        public DictionaryMetadata Build()
        {
            return new DictionaryMetadata(attrs);
        }

        public IDictionary<DictionaryAttribute, string> ToDictionary()
        {
            return new Dictionary<DictionaryAttribute, string>(attrs);
        }
    }
}
