using Morfologik.TestFramework;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Morfologik.Stemming
{
    public class DictionaryMetadataBuilderTest : TestCase
    {
        [Test]
        public void TestAllConstantsHaveBuilderMethods()
        {
            ICollection<DictionaryAttribute> keySet = new DictionaryMetadataBuilder()
                .ConvertCase()
                .Encoding(Encoding.Default)
                .Encoding("UTF-8")
                .FrequencyIncluded()
                .IgnoreAllUppercase()
                .IgnoreCamelCase()
                .IgnoreDiacritics()
                .IgnoreNumbers()
                .IgnorePunctuation()
                .Separator('+')
                .SupportRunOnWords()
                .Encoder(EncoderType.Suffix)
                .WithEquivalentChars(new Dictionary<char, IList<char>>())  //Collections.< Character, List < Character >> emptyMap())
                .WithReplacementPairs(new Dictionary<string, IList<string>>()) //Collections.< String, List < String >> emptyMap())
                .WithInputConversionPairs(new Dictionary<string, string>()) //Collections.< String, String > emptyMap())
                .WithOutputConversionPairs(new Dictionary<string, string>()) //Collections.< String, String > emptyMap())
                .Culture(CultureInfo.CurrentCulture)
                .License("")
                .Author("")
                .CreationDate("")
                .ToDictionary()
                .Keys;

            //Set<DictionaryAttribute> all = EnumSet.allOf(DictionaryAttribute.class);
            //all.removeAll(keySet);

            var all = new HashSet<DictionaryAttribute>((DictionaryAttribute[])Enum.GetValues(typeof(DictionaryAttribute)));
            all.ExceptWith(keySet);

            assertFalse(all.Any());

            //Assertions.assertThat(all).isEmpty();
        }
    }
}
