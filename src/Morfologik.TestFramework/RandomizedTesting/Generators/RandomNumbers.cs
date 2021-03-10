using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Morfologik.TestFramework.RandomizedTesting.Generators
{
    /// <summary>
    /// Utility classes for selecting random numbers from within a range or the 
    /// numeric domain for a given type.
    /// </summary>
    /// <seealso cref="BiasedNumbers"/>
    public static class RandomNumbers
    {
        /// <summary>
        /// A random integer from <paramref name="min"/> to <paramref name="max"/> (inclusive).
        /// </summary>
        public static int RandomInt32Between(Random random, int min, int max)
        {
            Debug.Assert(min <= max, String.Format("Min must be less than or equal max int. min: {0}, max: {1}", min, max));
            var range = max - min;
            if (range < Int32.MaxValue)
                return min + random.Next(1 + range);

            return min + (int)Math.Round(random.NextDouble() * range);
        }

        /* .NET has random.Next(max) which negates the need for randomInt(Random random, int max) as  */

        //        /** 
        //* A random integer between <code>min</code> (inclusive) and <code>max</code> (inclusive).
        //*/
        //        public static int RandomIntBetween(Random r, int min, int max)
        //        {
        //            Debug.Assert(max >= min, "max must be >= min: " + min + ", " + max);
        //            long range = (long)max - (long)min;
        //            if (range < int.MaxValue)
        //            {
        //                return min + r.Next(1 + (int)range);
        //            }
        //            else
        //            {
        //                return ToIntExact(min + NextLong(r, 1 + range));
        //            }
        //        }

        //        /** 
        //         * A random long between <code>min</code> (inclusive) and <code>max</code> (inclusive).
        //         */
        //        public static long TandomLongBetween(Random r, long min, long max)
        //        {
        //            Debug.Assert(max >= min , "max must be >= min: " + min + ", " + max);
        //            long range = max - min;
        //            if (range < 0)
        //            {
        //                range -= long.MaxValue;
        //                if (range == long.MinValue)
        //                {
        //                    // Full spectrum.
        //                    return r.NextInt64();
        //                }
        //                else
        //                {
        //                    long first = r.NextInt64() & long.MaxValue;
        //                    long second = range == long.MaxValue ? (r.NextInt64() & long.MaxValue) : NextLong(r, range + 1);
        //                    return min + first + second;
        //                }
        //            }
        //            else
        //            {
        //                long second = range == long.MaxValue ? (r.NextInt64() & long.MaxValue) : NextLong(r, range + 1);
        //                return min + second;
        //            }
        //        }

        //        /**
        //         * Similar to {@link Random#nextInt(int)}, but returns a long between
        //         * 0 (inclusive) and <code>n</code> (exclusive).
        //         * 
        //         * @param rnd Random generator.
        //         * @param n the bound on the random number to be returned.  Must be
        //         *        positive.
        //         * @return Returns a random number between 0 and n-1. 
        //         */
        //        public static long NextLong(Random rnd, long n)
        //        {
        //            if (n <= 0)
        //            {
        //                throw new ArgumentException("n <= 0: " + n);
        //            }

        //            long value = rnd.NextInt64();
        //            long range = n - 1;
        //            if ((n & range) == 0L)
        //            {
        //                value &= range;
        //            }
        //            else
        //            {
        //                for (long u = value.TripleShift(1); u + range - (value = u % n) < 0L;)
        //                {
        //                    u = rnd.NextInt64().TripleShift(1);
        //                }
        //            }
        //            return value;
        //        }

        //        private static int ToIntExact(long value)
        //        {
        //            if (value > int.MaxValue)
        //            {
        //                throw new ArithmeticException("Overflow: " + value);
        //            }
        //            else
        //            {
        //                return (int)value;
        //            }
        //        }
    }
}
