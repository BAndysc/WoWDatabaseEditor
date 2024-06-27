using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ProtoZeroSharp;
using WDE.Common;

namespace WDE.PacketViewer.Utils;

public unsafe class RefCountedArena : IDisposable
{
    private HashSet<Ref> counts = new();

    private Ref @ref;

    private ArenaAllocator* array;

    public RefCountedArena()
    {
        array = (ArenaAllocator*)Marshal.AllocHGlobal(sizeof(ArenaAllocator));
        *array = new ArenaAllocator();
        @ref = Increment();
    }

    ~RefCountedArena()
    {
        if (array != null)
        {
            LOG.LogCritical($"{nameof(RefCountedArena)} was finalized, but the underlying not freed, this is a memory leak");
        }
    }

    public void Dispose()
    {
        @ref.Dispose();
    }

    public Ref Increment()
    {
        lock (this)
        {
            if (array == null)
                throw new NullReferenceException($"Trying to get a ref to {nameof(RefCountedArena)}, but this is freed already");

            var refCount = new Ref(this);
            counts.Add(refCount);
            return refCount;
        }
    }

    private void Decrement(Ref @ref)
    {
        lock (this)
        {
            if (!counts.Contains(@ref))
            {
                LOG.LogError($"Double free of {nameof(RefCountedArena)}. Avoid this");
            }
            else
            {
                counts.Remove(@ref);
                if (counts.Count == 0)
                {
                    LOG.LogInformation($"Freeing {nameof(RefCountedArena)}, because all references were freed, total memory allocated: " + array->GetTotalLength() + " B");
                    Free();
                }
            }
        }
    }

    private void Free()
    {
        array->Free();
        Marshal.FreeHGlobal(new IntPtr(array));
        array = null;
    }

    public class Ref : System.IDisposable
    {
        private readonly RefCountedArena array;

        public ref ArenaAllocator Array => ref *array.array;

        public ArenaAllocator* Ptr => array.array;

        public Ref(RefCountedArena array)
        {
            this.array = array;
        }

        public void Dispose()
        {
            array.Decrement(this);
        }
    }
}