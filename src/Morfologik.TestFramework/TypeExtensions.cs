using J2N;
using System;
using System.IO;

namespace Morfologik.TestFramework
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Locates resources in the same directory as this type
        /// </summary>
        public static Stream getResourceAsStream(this Type t, string name)
        {
            return t.Assembly.FindAndGetManifestResourceStream(t, name);
        }
    }
}
