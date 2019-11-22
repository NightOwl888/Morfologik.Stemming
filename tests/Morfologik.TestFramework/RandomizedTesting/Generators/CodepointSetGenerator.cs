using J2N;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Text;

namespace Morfologik.TestFramework.RandomizedTesting.Generators
{
    /// <summary>
    /// A string generator from a predefined set of codepoints or characters.
    /// </summary>
    public class CodepointSetGenerator //: StringGenerator
    {
        private readonly int[] bmp;
        private readonly int[] supplementary;
        private readonly int[] all;

        /**
         * All characters must be from BMP (no parts of surrogate pairs allowed).
         */
        public CodepointSetGenerator(char[] chars)
        {
            this.bmp = new int[chars.Length];
            this.supplementary = new int[0];

            for (int i = 0; i < chars.Length; i++)
            {
                bmp[i] = ((int)chars[i]) & 0xffff;

                if (IsSurrogate(chars[i]))
                {
                    throw new ArgumentException("Value is part of a surrogate pair: 0x"
                        + bmp[i].ToHexString());
                }
            }

            this.all = Concat(bmp, supplementary);
            if (all.Length == 0)
            {
                throw new ArgumentException("Empty set of characters?");
            }
        }

        /**
         * Parse the given {@link String} and split into BMP and supplementary codepoints.
         */
        public CodepointSetGenerator(string s)
        {
            int bmps = 0;
            int supplementaries = 0;
            for (int i = 0; i < s.Length;)
            {
                int codepoint = s.CodePointAt(i);
                if (/*Character.*/IsSupplementaryCodePoint(codepoint))
                {
                    supplementaries++;
                }
                else
                {
                    bmps++;
                }

                i += /*Character.*/CharCount(codepoint);
            }

            this.bmp = new int[bmps];
            this.supplementary = new int[supplementaries];
            for (int i = 0; i < s.Length;)
            {
                int codepoint = s.CodePointAt(i);
                if (/*Character.*/IsSupplementaryCodePoint(codepoint))
                {
                    supplementary[--supplementaries] = codepoint;
                }
                else
                {
                    bmp[--bmps] = codepoint;
                }

                i += /*Character.*/CharCount(codepoint);
            }

            this.all = Concat(bmp, supplementary);
            if (all.Length == 0)
            {
                throw new ArgumentException("Empty set of characters?");
            }
        }

        public /*override*/ string OfCodeUnitsLength(System.Random r, int minCodeUnits, int maxCodeUnits)
        {
            int length = RandomNumbers.RandomInt32Between(r, minCodeUnits, maxCodeUnits);

            // Check and cater for odd number of code units if no bmp characters are given.
            if (bmp.Length == 0 && IsOdd(length))
            {
                if (minCodeUnits == maxCodeUnits)
                {
                    throw new ArgumentException("Cannot return an odd number of code units "
                        + " when surrogate pairs are the only available codepoints.");
                }
                else
                {
                    // length is odd so we move forward or backward to the closest even number.
                    if (length == minCodeUnits)
                    {
                        length++;
                    }
                    else
                    {
                        length--;
                    }
                }
            }

            //int[] codepoints = new int[length];
            char[] chars = new char[length * 2];
            int actual = 0;
            while (length > 0)
            {
                int cp;
                if (length == 1)
                {
                    //codepoints[actual] = bmp[r.Next(bmp.Length)];
                    cp = bmp[r.Next(bmp.Length)];
                }
                else
                {
                    //codepoints[actual] = all[r.Next(all.Length)];
                    cp = all[r.Next(all.Length)];
                }
                char[] temp = ToChars(cp);
                for (int i = 0; i < temp.Length; i++)
                    chars[actual++] = temp[i];

                //if (/*Character.*/IsSupplementaryCodePoint(codepoints[actual]))
                if (/*Character.*/IsSupplementaryCodePoint(cp))
                {
                    length -= 2;
                }
                else
                {
                    length -= 1;
                }
                //actual++;
            }
            //return new string(codepoints, 0, actual);
            return new string(chars, 0, actual);
        }

        //public override string OfCodePointsLength(System.Random r, int minCodePoints, int maxCodePoints)
        //{
        //    int length = RandomNumbers.RandomInt32Between(r, minCodePoints, maxCodePoints);
        //    int[] codepoints = new int[length];
        //    while (length > 0)
        //    {
        //        codepoints[--length] = all[r.Next(all.Length)];
        //    }
        //    return new string(codepoints, 0, codepoints.Length);
        //}

        /** Is a given number odd? */
        private static bool IsOdd(int v)
        {
            return (v & 1) != 0;
        }

        private int[] Concat(params int[][] arrays)
        {
            int totalLength = 0;
            foreach (int[] a in arrays) totalLength += a.Length;
            int[] concat = new int[totalLength];
            for (int i = 0, j = 0; j < arrays.Length;)
            {
                System.Array.Copy(arrays[j], 0, concat, i, arrays[j].Length);
                i += arrays[j].Length;
                j++;
            }
            return concat;
        }

        private bool IsSurrogate(char chr)
        {
            return (chr >= 0xd800 && chr <= 0xdfff);
        }

        #region From Character Class

        private const int MaxCodePoint = 0x10FFFF;
        private const int MinCodePoint = 0x000000;
        private const int MinSupplementaryCodePoint = 0x010000;

        private static int CharCount(int codePoint)
        {
            // A given codepoint can be represented in .NET either by 1 char (up to UTF16),
            // or by if it's a UTF32 codepoint, in which case the current char will be a surrogate
            return codePoint >= MinSupplementaryCodePoint ? 2 : 1;
        }
        private static bool IsSupplementaryCodePoint(int codePoint)
        {
            return (MinSupplementaryCodePoint <= codePoint && MaxCodePoint >= codePoint);
        }

        private static bool IsValidCodePoint(int codePoint)
        {
            return (MinCodePoint <= codePoint && MaxCodePoint >= codePoint);
        }

        private static char[] ToChars(int codePoint)
        {
            if (!IsValidCodePoint(codePoint))
            {
                throw new ArgumentException();
            }

            if (IsSupplementaryCodePoint(codePoint))
            {
                int cpPrime = codePoint - 0x10000;
                int high = 0xD800 | ((cpPrime >> 10) & 0x3FF);
                int low = 0xDC00 | (cpPrime & 0x3FF);
                return new char[] { (char)high, (char)low };
            }
            return new char[] { (char)codePoint };
        }

        #endregion
    }
}
