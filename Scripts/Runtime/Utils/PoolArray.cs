using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace BrunoMikoski.Core
{
    public class PoolArray<T> : IList<T>
    {
        private T[] data;
        private int usedCount;

#if DEBUG
        private int frameCountAtUpdate;
#endif

        public PoolArray(T[] data)
        {
            this.data = data;
        }

        public void SetUsedCount(int count)
        {
            if (count > data.Length)
            {
#if DEBUG
                // If this exception happens, please increase the PhysicsEx.ARRAY_SIZE value
                throw new OutOfMemoryException(string.Format(
                    "Physics operation yielded more results "
                    + "than available array space. Available: {0} Needed: {1}", data.Length,
                    count));
#else
                count = data.Length;
#endif
            }

            usedCount = count;
#if DEBUG
            frameCountAtUpdate = Time.frameCount;
#endif
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly PoolArray<T> target;
            private int index;

            public Enumerator(PoolArray<T> target)
            {
                this.target = target;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < target.usedCount;
            }

            public void Reset()
            {
                index = -1;
            }

            public T Current
            {
                get
                {
#if DEBUG
                    target.AssertCanBeUsed();
                    if (index < 0)
                        throw new InvalidOperationException("Call MoveNext before calling Current");

                    if (index >= target.usedCount)
                    {
                        throw new IndexOutOfRangeException(string.Format(
                            "Index {0} is out of range "
                            + "0 - {1}", index, target.usedCount - 1));
                    }
#endif
                    return target.data[index];
                }
            }
            object IEnumerator.Current { get { return Current; } }

            void IDisposable.Dispose() { }
        }

        public IEnumerator<T> GetEnumerator()
        {
#if DEBUG
            AssertCanBeUsed();
#endif
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
#if DEBUG
            AssertCanBeUsed();
#endif
            Array.Clear(data, 0, data.Length);
            usedCount = 0;
        }

        public bool Contains(T item)
        {
#if DEBUG
            AssertCanBeUsed();
#endif
            return Array.IndexOf(data, item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
#if DEBUG
            AssertCanBeUsed();
#endif
            Array.Copy(this.data, 0, array, arrayIndex, usedCount);
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public T[] Data { get { return data; } }

        public int Count
        {
            get
            {
#if DEBUG
                AssertCanBeUsed();
#endif
                return usedCount;
            }
        }
        public bool IsReadOnly
        {
            get
            {
#if DEBUG
                AssertCanBeUsed();
#endif
                return true;
            }
        }

        public int IndexOf(T item)
        {
#if DEBUG
            AssertCanBeUsed();
#endif
            return Array.IndexOf(data, item, 0, usedCount);
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public T this[int index]
        {
            get
            {
#if DEBUG
                AssertCanBeUsed();
#endif
                if (index < 0 || index >= usedCount)
                {
                    throw new IndexOutOfRangeException($"Index {index} is out of range " + $"0 - {usedCount - 1}");
                }
                return data[index];
            }
            set { throw new NotSupportedException(); }
        }

#if DEBUG
        public void AssertCanBeUsed()
        {
            Assert.IsTrue(Time.frameCount == frameCountAtUpdate,
                "PhysicsResult is used over multiple "
                + "frames. Only use PhysicsResult in a single frame.");
        }
#endif
    }
}
