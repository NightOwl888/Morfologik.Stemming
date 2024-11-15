using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morfologik.Fsa.Support
{

    internal static class EncodingProviderInitializer
    {
        private static bool _isInitialized;

        [Conditional("FEATURE_ENCODINGPROVIDERS")]
        public static void EnsureInitialized()
        {
#if FEATURE_ENCODINGPROVIDERS
            if (!_isInitialized)
            {
                // Support for iso-8859-1 encoding. See: https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider?view=netcore-2.0
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                _isInitialized = true;
            }
#endif
        }
    }
}
