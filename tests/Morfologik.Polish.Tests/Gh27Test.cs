using Morfologik.TestFramework;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Morfologik.Stemming.Polish.Tests
{
    public class Gh27Test : TestCase
    {
        /* */
        [Test]
        public void Test_Gh27()
        {
            PolishStemmer stemmer = new PolishStemmer();

            string input = "Nie zabrakło oczywiście wpadek. Największym zaskoczeniem okazał się dla nas strój Katarzyny Zielińskiej, której ewidentnie o coś chodziło, ale wciąż nie wiemy o co.";

            DictionaryLookupResult reuse = new();
            foreach (string t in Regex.Split(input.ToLower(new CultureInfo("pl")), "[\\s\\.\\,]+"))
            {
                Console.Out.WriteLine("> '" + t + "'");
                foreach (WordData2 wd in stemmer.Lookup(t.AsSpan(), reuse))
                {
                    Console.Out.WriteLine(
                        "  - " +
                        (wd.Stem.IsEmpty ? "<null>" : wd.Stem.ToString()) + ", " + wd.Tag.ToString());
                }
                Console.Out.WriteLine();
            }
        }
    }
}
