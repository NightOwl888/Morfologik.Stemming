using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using StringBuffer = System.Text.StringBuilder;

namespace Morfologik.Stemming
{
    // TODO: Move this into J2N and eliminate
    public static class PropertyExtensionMethods
    {
        public static string GetProperty(this IDictionary<string, string> properties, string key, string defaultValue)
        {
            if (properties.TryGetValue(key, out string prop))
                return prop;
            return defaultValue;
        }

        public static void Load(this IDictionary<string, string> properties, TextReader reader)
        {
            lock (properties.GetSyncRoot())
            {
                LoadProperties(properties, new LineReader(reader));
            }
        }

        public static void Load(this IDictionary<string, string> properties, Stream input)
        {
            lock (properties.GetSyncRoot())
            {
                LoadProperties(properties, new LineReader(input));
            }
        }

        private static object GetSyncRoot(this IDictionary<string, string> dictionary)
        {
            var collection = dictionary as ICollection;
            if (collection != null && collection.IsSynchronized)
                return ((ICollection)dictionary).SyncRoot;
            else
                return dictionary;
        }

        private static void LoadProperties(IDictionary<string, string> properties, LineReader lr)
        {
            char[] convtBuf = new char[1024];
            int limit;
            int keyLen;
            int valueStart;
            char c;
            bool hasSep;
            bool precedingBackslash;

            while ((limit = lr.ReadLine()) >= 0)
            {
                c = (char)0;
                keyLen = 0;
                valueStart = limit;
                hasSep = false;

                //System.out.println("line=<" + new String(lineBuf, 0, limit) + ">");
                precedingBackslash = false;
                while (keyLen < limit)
                {
                    c = lr.lineBuf[keyLen];
                    //need check if escaped.
                    if ((c == '=' || c == ':') && !precedingBackslash)
                    {
                        valueStart = keyLen + 1;
                        hasSep = true;
                        break;
                    }
                    else if ((c == ' ' || c == '\t' || c == '\f') && !precedingBackslash)
                    {
                        valueStart = keyLen + 1;
                        break;
                    }
                    if (c == '\\')
                    {
                        precedingBackslash = !precedingBackslash;
                    }
                    else
                    {
                        precedingBackslash = false;
                    }
                    keyLen++;
                }
                while (valueStart < limit)
                {
                    c = lr.lineBuf[valueStart];
                    if (c != ' ' && c != '\t' && c != '\f')
                    {
                        if (!hasSep && (c == '=' || c == ':'))
                        {
                            hasSep = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                    valueStart++;
                }
                string key = LoadConvert(lr.lineBuf, 0, keyLen, convtBuf);
                string value = LoadConvert(lr.lineBuf, valueStart, limit - valueStart, convtBuf);
                properties[key] = value;
            }
        }

        /* Read in a "logical line" from an InputStream/Reader, skip all comment
     * and blank lines and filter out those leading whitespace characters
     * (\u0020, \u0009 and \u000c) from the beginning of a "natural line".
     * Method returns the char length of the "logical line" and stores
     * the line in "lineBuf".
     */
        private class LineReader
        {
            public LineReader(Stream inStream)
            {
                this.inStream = inStream;
                inByteBuf = new byte[8192];
            }

            public LineReader(TextReader reader)
            {
                this.reader = reader;
                inCharBuf = new char[8192];
            }

            internal byte[] inByteBuf;
            internal char[] inCharBuf;
            internal char[] lineBuf = new char[1024];
            internal int inLimit = 0;
            internal int inOff = 0;
            internal Stream inStream;
            internal TextReader reader;

            internal int ReadLine()
            {
                int len = 0;
                char c = (char)0;

                bool skipWhiteSpace = true;
                bool isCommentLine = false;
                bool isNewLine = true;
                bool appendedLineBegin = false;
                bool precedingBackslash = false;
                bool skipLF = false;

                while (true)
                {
                    if (inOff >= inLimit)
                    {
                        inLimit = (inStream == null) ? reader.Read(inCharBuf, 0, inCharBuf.Length)
                                                  : inStream.Read(inByteBuf, 0, inByteBuf.Length);
                        inOff = 0;
                        if (inLimit <= 0)
                        {
                            if (len == 0 || isCommentLine)
                            {
                                return -1;
                            }
                            return len;
                        }
                    }
                    if (inStream != null)
                    {
                        //The line below is equivalent to calling a
                        //ISO8859-1 decoder.
                        c = (char)(0xff & inByteBuf[inOff++]);
                    }
                    else
                    {
                        c = inCharBuf[inOff++];
                    }
                    if (skipLF)
                    {
                        skipLF = false;
                        if (c == '\n')
                        {
                            continue;
                        }
                    }
                    if (skipWhiteSpace)
                    {
                        if (c == ' ' || c == '\t' || c == '\f')
                        {
                            continue;
                        }
                        if (!appendedLineBegin && (c == '\r' || c == '\n'))
                        {
                            continue;
                        }
                        skipWhiteSpace = false;
                        appendedLineBegin = false;
                    }
                    if (isNewLine)
                    {
                        isNewLine = false;
                        if (c == '#' || c == '!')
                        {
                            isCommentLine = true;
                            continue;
                        }
                    }

                    if (c != '\n' && c != '\r')
                    {
                        lineBuf[len++] = c;
                        if (len == lineBuf.Length)
                        {
                            int newLength = lineBuf.Length * 2;
                            if (newLength < 0)
                            {
                                newLength = int.MaxValue;
                            }
                            char[] buf = new char[newLength];
                            System.Array.Copy(lineBuf, 0, buf, 0, lineBuf.Length);
                            lineBuf = buf;
                        }
                        //flip the preceding backslash flag
                        if (c == '\\')
                        {
                            precedingBackslash = !precedingBackslash;
                        }
                        else
                        {
                            precedingBackslash = false;
                        }
                    }
                    else
                    {
                        // reached EOL
                        if (isCommentLine || len == 0)
                        {
                            isCommentLine = false;
                            isNewLine = true;
                            skipWhiteSpace = true;
                            len = 0;
                            continue;
                        }
                        if (inOff >= inLimit)
                        {
                            inLimit = (inStream == null)
                                      ? reader.Read(inCharBuf, 0, inCharBuf.Length)
                                      : inStream.Read(inByteBuf, 0, inByteBuf.Length);
                            inOff = 0;
                            if (inLimit <= 0)
                            {
                                return len;
                            }
                        }
                        if (precedingBackslash)
                        {
                            len -= 1;
                            //skip the leading whitespace characters in following line
                            skipWhiteSpace = true;
                            appendedLineBegin = true;
                            precedingBackslash = false;
                            if (c == '\r')
                            {
                                skipLF = true;
                            }
                        }
                        else
                        {
                            return len;
                        }
                    }
                }
            }
        }

        /*
     * Converts encoded &#92;uxxxx to unicode chars
     * and changes special saved chars to their original forms
     */
        private static string LoadConvert(char[] input, int off, int len, char[] convtBuf)
        {
            if (convtBuf.Length < len)
            {
                int newLen = len * 2;
                if (newLen < 0)
                {
                    newLen = int.MaxValue;
                }
                convtBuf = new char[newLen];
            }
            char aChar;
            char[] output = convtBuf;
            int outLen = 0;
            int end = off + len;

            while (off < end)
            {
                aChar = input[off++];
                if (aChar == '\\')
                {
                    aChar = input[off++];
                    if (aChar == 'u')
                    {
                        // Read the xxxx
                        int value = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            aChar = input[off++];
                            switch (aChar)
                            {
                                case '0':
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                case '8':
                                case '9':
                                    value = (value << 4) + aChar - '0';
                                    break;
                                case 'a':
                                case 'b':
                                case 'c':
                                case 'd':
                                case 'e':
                                case 'f':
                                    value = (value << 4) + 10 + aChar - 'a';
                                    break;
                                case 'A':
                                case 'B':
                                case 'C':
                                case 'D':
                                case 'E':
                                case 'F':
                                    value = (value << 4) + 10 + aChar - 'A';
                                    break;
                                default:
                                    throw new ArgumentException(
                                                 "Malformed \\uxxxx encoding.");
                            }
                        }
                        output[outLen++] = (char)value;
                    }
                    else
                    {
                        if (aChar == 't') aChar = '\t';
                        else if (aChar == 'r') aChar = '\r';
                        else if (aChar == 'n') aChar = '\n';
                        else if (aChar == 'f') aChar = '\f';
                        output[outLen++] = aChar;
                    }
                }
                else
                {
                    output[outLen++] = aChar;
                }
            }
            return new string(output, 0, outLen);
        }

        /*
     * Converts unicodes to encoded &#92;uxxxx and escapes
     * special characters with a preceding slash
     */
        private static string SaveConvert(string theString,
                                   bool escapeSpace,
                                   bool escapeUnicode)
        {
            int len = theString.Length;
            int bufLen = len * 2;
            if (bufLen < 0)
            {
                bufLen = int.MaxValue;
            }
            StringBuffer outBuffer = new StringBuffer(bufLen);

            for (int x = 0; x < len; x++)
            {
                char aChar = theString[x];
                // Handle common case first, selecting largest block that
                // avoids the specials below
                if ((aChar > 61) && (aChar < 127))
                {
                    if (aChar == '\\')
                    {
                        outBuffer.Append('\\'); outBuffer.Append('\\');
                        continue;
                    }
                    outBuffer.Append(aChar);
                    continue;
                }
                switch (aChar)
                {
                    case ' ':
                        if (x == 0 || escapeSpace)
                            outBuffer.Append('\\');
                        outBuffer.Append(' ');
                        break;
                    case '\t':
                        outBuffer.Append('\\'); outBuffer.Append('t');
                        break;
                    case '\n':
                        outBuffer.Append('\\'); outBuffer.Append('n');
                        break;
                    case '\r':
                        outBuffer.Append('\\'); outBuffer.Append('r');
                        break;
                    case '\f':
                        outBuffer.Append('\\'); outBuffer.Append('f');
                        break;
                    case '=': // Fall through
                    case ':': // Fall through
                    case '#': // Fall through
                    case '!':
                        outBuffer.Append('\\'); outBuffer.Append(aChar);
                        break;
                    default:
                        if (((aChar < 0x0020) || (aChar > 0x007e)) & escapeUnicode)
                        {
                            outBuffer.Append('\\');
                            outBuffer.Append('u');
                            outBuffer.Append(ToHex((aChar >> 12) & 0xF));
                            outBuffer.Append(ToHex((aChar >> 8) & 0xF));
                            outBuffer.Append(ToHex((aChar >> 4) & 0xF));
                            outBuffer.Append(ToHex(aChar & 0xF));
                        }
                        else
                        {
                            outBuffer.Append(aChar);
                        }
                        break;
                }
            }
            return outBuffer.ToString();
        }

        private static void WriteComments(TextWriter bw, string comments)
        {
            bw.Write("#");
            int len = comments.Length;
            int current = 0;
            int last = 0;
            char[] uu = new char[6];
            uu[0] = '\\';
            uu[1] = 'u';
            while (current < len)
            {
                char c = comments[current];
                if (c > '\u00ff' || c == '\n' || c == '\r')
                {
                    if (last != current)
                        bw.Write(comments.Substring(last, current - last)); // end - start
                    if (c > '\u00ff')
                    {
                        uu[2] = ToHex((c >> 12) & 0xf);
                        uu[3] = ToHex((c >> 8) & 0xf);
                        uu[4] = ToHex((c >> 4) & 0xf);
                        uu[5] = ToHex(c & 0xf);
                        bw.Write(new String(uu));
                    }
                    else
                    {
                        bw.WriteLine();
                        if (c == '\r' &&
                            current != len - 1 &&
                            comments[current + 1] == '\n')
                        {
                            current++;
                        }
                        if (current == len - 1 ||
                            (comments[current + 1] != '#' &&
                            comments[current + 1] != '!'))
                            bw.Write("#");
                    }
                    last = current + 1;
                }
                current++;
            }
            if (last != current)
                bw.Write(comments.Substring(last, current - last)); // end - start
            bw.WriteLine();
        }

        public static void Store(this IDictionary<string, string> properties, TextWriter writer, string comments)
        {
            Store0(properties, writer,
               comments,
               false);
        }

        public static void Store(this IDictionary<string, string> properties, Stream output, string comments)
        {
            using (var sw = new StreamWriter(output, Encoding.GetEncoding("iso-8859-1"), 1024, true))
                Store0(properties, sw, comments, true);
        }

        private static void Store0(IDictionary<string, string> properties, TextWriter bw, string comments, bool escUnicode)
        {
            if (comments != null)
            {
                WriteComments(bw, comments);
            }
            bw.Write("#");
            bw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            lock (properties.GetSyncRoot())
            {
                foreach (var prop in properties)
                {
                    string key = prop.Key;
                    string val = prop.Value;
                    key = SaveConvert(key, true, escUnicode);
                    /* No need to escape embedded and trailing spaces for value, hence
                     * pass false to flag.
                     */
                    val = SaveConvert(val, false, escUnicode);
                    bw.Write(key + "=" + val);
                    bw.WriteLine();
                }
            }
            bw.Flush();
        }


        /**
         * Convert a nibble to a hex character
         * @param   nibble  the nibble to convert.
         */
        private static char ToHex(int nibble)
        {
            return hexDigit[(nibble & 0xF)];
        }

        /** A table of hex digits */
        private static readonly char[] hexDigit = {
            '0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F'
        };


        ///// <summary>
        ///// Searches for the first index of the specified character. The search for
        ///// the character starts at the beginning and moves towards the end.
        ///// </summary>
        ///// <param name="text">This <see cref="StringBuilder"/>.</param>
        ///// <param name="value">The string to find.</param>
        ///// <returns>The index of the specified character, or -1 if the character isn't found.</returns>
        ///// <exception cref="ArgumentNullException">If <paramref name="text"/> or <paramref name="value"/> is <c>null</c>.</exception>
        //public static int IndexOf(this StringBuilder text, string value)
        //{
        //    return IndexOf(text, value, 0);
        //}

        ///// <summary>
        ///// Searches for the index of the specified character. The search for the
        ///// character starts at the specified offset and moves towards the end.
        ///// </summary>
        ///// <param name="text">This <see cref="StringBuilder"/>.</param>
        ///// <param name="value">The string to find.</param>
        ///// <param name="startIndex">The starting offset.</param>
        ///// <returns>The index of the specified character, or -1 if the character isn't found.</returns>
        ///// <exception cref="ArgumentNullException">If <paramref name="text"/> or <paramref name="value"/> is <c>null</c>.</exception>
        //public static int IndexOf(this StringBuilder text, string value, int startIndex)
        //{
        //    if (text == null)
        //        throw new ArgumentNullException(nameof(text));
        //    if (value == null)
        //        throw new ArgumentNullException(nameof(value));

        //    int index;
        //    int length = value.Length;
        //    int maxSearchLength = (text.Length - length) + 1;

        //    for (int i = startIndex; i < maxSearchLength; ++i)
        //    {
        //        if (text[i] == value[0])
        //        {
        //            index = 1;
        //            while ((index < length) && (text[i + index] == value[index]))
        //                ++index;

        //            if (index == length)
        //                return i;
        //        }
        //    }

        //    return -1;
        //}
    }
}
