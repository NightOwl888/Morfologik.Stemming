using Morfologik.TestFramework;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Morfologik.Stemming
{
    // Morofologik.Stemming: Refactored to copy the data from the embedded resource to a local folder for testing
    // and added test for URI overload
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

        [Test] // Morfologik.Stemming specific
        public async Task TestReadFromLocalHttpServer()
        {
            // Start a local HTTP server using HttpListener to serve files
            using (var httpServer = new HttpListener())
            {
                httpServer.Prefixes.Add("http://localhost:5000/");
                httpServer.Start();

                var serverTask = Task.Run(() => HandleRequests(httpServer));

                // Provide URLs to the local server files
                Uri dictUrl = new Uri("http://localhost:5000/test.dict");
                assertNotNull("Dictionary Read from HTTP server URI failed.", Dictionary.Read(dictUrl));

                httpServer.Stop();
                await serverTask; // Ensure server task completes
            }
        }

        private async Task HandleRequests(HttpListener listener)
        {
            while (listener.IsListening)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    string filePath = context.Request.RawUrl switch
                    {
                        "/test.dict" => dict,
                        "/test.info" => info,
                        _ => null
                    };

                    if (filePath != null && File.Exists(filePath))
                    {
                        context.Response.ContentType = "application/octet-stream";
                        using var fileStream = File.OpenRead(filePath);
                        await fileStream.CopyToAsync(context.Response.OutputStream);
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    }

                    context.Response.Close();
                }
                catch (Exception ex) when (ex is HttpListenerException || ex is TaskCanceledException)
                {
                    break; // Stop listener if an error or cancellation occurs
                }
            }
        }
    }
}
