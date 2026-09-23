using System;
using System.Diagnostics;

namespace Morfologik.Stemming
{
    /// <summary>
    /// Utilities to setup buffers.
    /// </summary>
    internal static class BufferUtils
    {
        // Morfologik.Stemming: Removed ClearAndEnsureCapacity() helper because we are using ArrayBufferWriter to manage buffer growth and reuse.

        // Morfologik.Stemming: Removed ToString() helper because System.Memory already has it.

        // Morfologik.Stemming: Removed ToArray() helper because System.Memory already has it.

        /// <summary>
        /// Compute the length of the shared prefix between two byte sequences.
        /// </summary>
        internal static int SharedPrefixLength(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            int i = 0;
            int max = Math.Min(a.Length, b.Length);
            while (i < max && a[i] == b[i])
            {
                i++;
            }
            return i;
        }

        internal static int SharedPrefixLengthAfterRemoving(
            ReadOnlySpan<byte> source,
            int removeIndex,
            int removeLength,
            ReadOnlySpan<byte> target)
        {
            Debug.Assert((uint)removeIndex <= (uint)source.Length, "removeIndex must be within the bounds of source");
            Debug.Assert((uint)removeIndex <= (uint)source.Length - (uint)removeLength, "removeIndex and removeLength must specify a valid range within source");

            int sourceLength = source.Length - removeLength;
            int length = Math.Min(sourceLength, target.Length);

            int i = 0;
            while (i < length)
            {
                int sourceIndex = i < removeIndex
                    ? i
                    : i + removeLength;

                if (source[sourceIndex] != target[i])
                    break;

                i++;
            }

            return i;
        }
    }
}
