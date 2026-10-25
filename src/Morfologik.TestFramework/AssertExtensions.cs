using NUnit.Framework;
using System;

namespace Morfologik.TestFramework
{
    public class AssertExtensions
    {
        public static T Throws<T>(string expectedParamName, Action action)
            where T : ArgumentException
        {
            T? exception = Assert.Throws<T>(() => action());

            Assert.AreEqual(expectedParamName, exception?.ParamName);

            return exception!;
        }
    }
}
