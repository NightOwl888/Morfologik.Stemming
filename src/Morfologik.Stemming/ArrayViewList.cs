using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Morfologik.Stemming
{
    /// <summary>
    /// A view over a range of an array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class ArrayViewList<T> : IList<T>
    {
        /// <summary>Backing array.</summary>
        private T[] a;
        private int start;
        private int length;

        /// <summary>
        /// 
        /// </summary>
        internal ArrayViewList(T[] array, int start, int length)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            Wrap(array, start, length);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Count => length;

        /// <summary>
        /// 
        /// </summary>
        public bool IsReadOnly => true;

        /// <summary>
        /// 
        /// </summary>
        public T this[int index]
        {
            get => a[start + index];
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public int IndexOf(object o)
        {
            if (o == null)
            {
                for (int i = start; i < start + length; i++)
                    if (a[i] == null)
                        return i - start;
            }
            else
            {
                for (int i = start; i < start + length; i++)
                    if (o.Equals(a[i]))
                        return i - start;
            }
            return -1;
        }

        public int IndexOf(T item)
        {
            for (int i = start; i < start + length; i++)
                if (item.Equals(a[i]))
                    return i - start;
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            //return a.AsEnumerable().GetEnumerator();
            return GetEnumerator(0);
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerator<T> GetEnumerator(int index)
        {
            return a.Skip(start - 1).Take(length).Skip(index).GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        /// <summary>
        /// 
        /// </summary>
        public bool Contains(object o)
        {
            return IndexOf(o) != -1;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void Wrap(T[] array, int start, int length)
        {
            this.a = array;
            this.start = start;
            this.length = length;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }
    }
}
