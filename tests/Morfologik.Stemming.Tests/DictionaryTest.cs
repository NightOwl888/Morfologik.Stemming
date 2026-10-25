using Morfologik.TestFramework;
using NUnit.Framework;
using System.IO;

namespace Morfologik.Stemming
{
    // Morofologik.Stemming: Refactored to copy the data from the embedded resource to a local folder for testing
    public class DictionaryTest : TestCase
    {
        private string tempDir;
        private string dict;
        private string info;

        [SetUp]
        public void Setup()
        {
            // Create temporary directory and dictionary paths
            tempDir = Path.Combine(Path.GetTempPath(), "MorfologikTest");
            Directory.CreateDirectory(tempDir);
            dict = Path.Combine(tempDir, "test.dict");
            info = Path.Combine(tempDir, "test.info");

            // Copy sample files to temporary directory
            using (var dictInput = this.GetType().getResourceAsStream("test-infix.dict"))
            using (var infoInput = this.GetType().getResourceAsStream("test-infix.info"))
            using (var dictOutput = new FileStream(dict, FileMode.Create))
            using (var infoOutput = new FileStream(info, FileMode.Create))
            {
                dictInput.CopyTo(dictOutput);
                infoInput.CopyTo(infoOutput);
            }
        }

        [TearDown]
        public void Cleanup()
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch { /* ignore */ }
        }

        [Test]
        public void TestReadFromFile()
        {
            assertNotNull(Dictionary.Read(dict));
        }
    }
}
