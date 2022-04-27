using System;
using System.Buffers;
using System.Collections;
using System.Diagnostics;

namespace WDE.MpqReader
{
    public class PooledArray<T> : System.IDisposable
    {
        private readonly T[] array;
        private int length;
        private bool disposed;

        public PooledArray(int length)
        {
            this.length = length;
            array = ArrayPool<T>.Shared.Rent(length);
        }

        ~PooledArray()
        {
            if (!disposed)
            {
                Console.WriteLine("PooledArray was not disposed");
                Dispose();
            }
        }
        
        public int Length => length;
        public bool IsDisposed => disposed;

        public T this[int index]
        {
            get
            {
                #if DEBUG
                Debug.Assert(!disposed, "Trying to read disposed PooledArray");
                #endif
                return array[index];
            }
            set => array[index] = value;
        }

        public ReadOnlySpan<T> AsSpan() => array.AsSpan(0, length);
    
        public T[] AsArray() => array;

        public void Dispose()
        {
            disposed = true;
            ArrayPool<T>.Shared.Return(array);
        }

        public void Shrink(int newLength)
        {
            Debug.Assert(newLength <= length);
            length = newLength;
        }
    }

    public struct GroupedArrayPooler<T> : System.IDisposable
    {
        private readonly T[][] arrays;
        private readonly int length;
        private int i = 0;

        public GroupedArrayPooler(int capacity)
        {
            length = capacity;
            i = 0;
            arrays = ArrayPool<T[]>.Shared.Rent(capacity);
        }

        public T[] Get(int size)
        {
            var array= ArrayPool<T>.Shared.Rent(size);
            if (i >= length)
                throw new Exception("This array pooler can create no more than " + length + " arrays");
            arrays[i++] = array;
            return array;
        }

        public void Dispose()
        {
            for (int j = 0; j < i; ++j)
            {
                ArrayPool<T>.Shared.Return(arrays[j]);
            }
            ArrayPool<T[]>.Shared.Return(arrays);
        }
    }
}