using System;

namespace WDE.PacketViewer.Utils;

public readonly unsafe struct Pointer<T> where T : unmanaged
{
    public readonly T* Ptr;

    public ref T Value
    {
        get
        {
            if (Ptr == null)
                throw new ArgumentNullException();
            return ref *Ptr;
        }
    }

    public Pointer(T* ptr)
    {
        Ptr = ptr;
    }

    public static implicit operator Pointer<T>(T* ptr) => new Pointer<T>(ptr);

    public static implicit operator T*(Pointer<T> ptr) => ptr.Ptr;
}