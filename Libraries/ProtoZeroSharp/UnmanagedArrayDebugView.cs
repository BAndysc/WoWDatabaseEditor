using System.Diagnostics;

namespace ProtoZeroSharp;

public class UnmanagedArrayDebugView<T> where T : unmanaged
{
    private readonly UnmanagedArray<T> _array;

    public UnmanagedArrayDebugView(UnmanagedArray<T> array)
    {
        _array = array;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            T[] array = new T[_array.Length];
            for (int i = 0; i < _array.Length; i++)
            {
                array[i] = _array[i];
            }
            return array;
        }
    }
}