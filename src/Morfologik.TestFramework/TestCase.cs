using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Morfologik.TestFramework
{
    [TestFixture]
    public abstract class TestCase
    {
        [SetUp]
        public virtual void SetUp()
        {
        }

        [TearDown]
        public virtual void TearDown()
        {
        }

        public static void assertTrue(bool condition)
        {
            Assert.IsTrue(condition);
        }

        public static void assertTrue(string message, bool condition)
        {
            Assert.IsTrue(condition, message);
        }

        public static void assertFalse(bool condition)
        {
            Assert.IsFalse(condition);
        }

        public static void assertFalse(string message, bool condition)
        {
            Assert.IsFalse(condition, message);
        }

        public static void assertEquals(object expected, object actual)
        {
            Assert.AreEqual(expected, actual);
        }

        public static void assertEquals(string message, object expected, object actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        public static void assertEquals(long expected, long actual)
        {
            Assert.AreEqual(expected, actual);
        }

        public static void assertEquals(string message, long expected, long actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        public static void assertEquals<T>(ISet<T> expected, ISet<T> actual)
        {
            Assert.True(expected.SetEquals(actual));
        }

        public static void assertEquals<T>(string message, ISet<T> expected, ISet<T> actual)
        {
            Assert.True(expected.SetEquals(actual), message);
        }

        public static void assertEquals<T, S>(IDictionary<T, S> expected, IDictionary<T, S> actual)
        {
            Assert.AreEqual(expected, actual);
        }

        public static void assertEquals(ICollection expected, ICollection actual)
        {
            Assert.AreEqual(expected, actual);
        }

        public static void assertNotSame(object unexpected, object actual)
        {
            Assert.AreNotSame(unexpected, actual);
        }

        public static void assertNotSame(string message, object unexpected, object actual)
        {
            Assert.AreNotSame(unexpected, actual, message);
        }

        public static void assertEquals(double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta);
        }

        public static void assertEquals(string msg, double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta, msg);
        }

        public static void assertNotNull(object o)
        {
            Assert.NotNull(o);
        }

        public static void assertNotNull(string msg, object o)
        {
            Assert.NotNull(o, msg);
        }

        public static void assertNull(object o)
        {
            Assert.Null(o);
        }

        public static void assertNull(string msg, object o)
        {
            Assert.Null(o, msg);
        }

        public static void assertArrayEquals<T>(T[] a1, T[] a2)
        {
            CollectionAssert.AreEqual(a1, a2);
        }

        public static void assertSame(Object expected, Object actual)
        {
            Assert.AreSame(expected, actual);
        }

        public static void assertSame(string message, Object expected, Object actual)
        {
            Assert.AreSame(expected, actual, message);
        }

        public static void fail()
        {
            Assert.Fail();
        }

        public static void fail(string message)
        {
            Assert.Fail(message);
        }


        public static System.Random Random => TestContext.CurrentContext.Random;

        /// <summary>
        /// True if and only if tests are run in verbose mode. If this flag is false
        /// tests are not expected to print any messages.
        /// </summary>
        public static readonly bool VERBOSE = ( //SystemProperties.GetPropertyAsBoolean("tests.verbose",
#if DEBUG
            true
#else
            false
#endif
);

        /// <summary>
        /// Pick a random object from the <paramref name="collection"/>.
        /// </summary>
        public static T randomFrom<T>(ICollection<T> collection)
        {
            if (collection.Count == 0)
            {
                throw new ArgumentException("Can't pick a random object from an empty collection.");
            }
            return collection.ElementAt(Random.Next(0, collection.Count));
        }

        private static readonly AsciiLettersGenerator asciiLettersGenerator = new AsciiLettersGenerator();


        public static string randomAsciiLettersOfLengthBetween(int minCodeUnits, int maxCodeUnits) => randomAsciiLettersOfLengthBetween(Random, minCodeUnits, maxCodeUnits);
        public static string randomAsciiLettersOfLengthBetween(Random r, int minCodeUnits, int maxCodeUnits) => asciiLettersGenerator.OfCodeUnitsLength(r, minCodeUnits, maxCodeUnits);
    }
}
