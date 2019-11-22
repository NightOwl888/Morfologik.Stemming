using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Morfologik.TestFramework
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Locates resources in the same directory as this type
        /// </summary>
        public static Stream getResourceAsStream(this Type t, string name)
        {
            return t.GetTypeInfo().Assembly.FindAndGetManifestResourceStream(t, name);
        }
    }
}
