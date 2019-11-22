using Morfologik.TestFramework;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Morfologik.Stemming
{
    public class DictionaryTest : TestCase
    {
        [Test]
        public void TestReadFromFile()
        {
            string tempDir = Path.GetTempPath();
            //Path tempDir = super.newTempDir();


            //Path dict = tempDir.resolve("odd name.dict");
            //Path info = dict.resolveSibling("odd name.info");
            string dict = Path.Combine(tempDir, "odd name.dict");
            string info = Path.Combine(tempDir, "odd name.info");
            using (Stream dictInput = this.GetType().getResourceAsStream("test-infix.dict"))
            using (Stream infoInput = this.GetType().getResourceAsStream("test-infix.info"))
            using (Stream dictOutput = new FileStream(dict, FileMode.OpenOrCreate))
            using (Stream infoOutput = new FileStream(info, FileMode.OpenOrCreate))
            {
                dictInput.CopyTo(dictOutput);
                infoInput.CopyTo(infoOutput);
                //Files.copy(dictInput, dict);
                //Files.copy(infoInput, info);
                //foreach (var file in Directory.GetFiles(dict))

            }

            //assertNotNull(Dictionary.Read(dict.toUri().toURL())); // TODO: Work out how to do URL
            assertNotNull(Dictionary.Read(dict));
        }
    }
}
