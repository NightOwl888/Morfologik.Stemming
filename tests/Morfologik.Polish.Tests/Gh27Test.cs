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
            foreach (string t in Regex.Split(input.ToLower(new CultureInfo("pl")), "[\\s\\.\\,]+"))
            {
                Console.Out.WriteLine("> '" + t + "'");
                foreach (WordData wd in stemmer.Lookup(t))
                {
                    Console.Out.WriteLine(
                        "  - " +
                        (wd.GetStem() == null ? "<null>" : wd.GetStem().ToString()) + ", " + wd.GetTag());
                }
                Console.Out.WriteLine();
            }
        }
    }
}
