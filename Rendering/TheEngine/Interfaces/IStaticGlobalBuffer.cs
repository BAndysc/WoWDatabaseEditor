namespace TheEngine.Interfaces;

/// <summary>
/// A STATIC global storage buffer bound at set 3. Unlike <see cref="IGlobalBuffer{T}"/> (which
/// is per-frame double-buffered and rewritten every frame for dynamic data), this has a single
/// persistent backing and is written only when its contents change (e.g. a terrain tile loads /
/// unloads). Memory is handed out in fixed-size slots (<see cref="SlotElementCount"/> elements
/// each) so allocation is a slab free-list rather than a general allocator.
///
/// Safety: a freshly <see cref="Allocate"/>d slot is not referenced by any in-flight draw until
/// the caller publishes its offset, so the initial write is race-free. <see cref="Free"/> defers
/// returning the slot to the free list until every submission that could still read it completes,
/// so a slot is never overwritten while the GPU is reading the previous tenant.
/// </summary>
public interface IStaticGlobalBuffer<T> where T : unmanaged
{
    /// <summary>Number of <typeparamref name="T"/> elements in each slot.</summary>
    int SlotElementCount { get; }

    /// <summary>Allocates one slot and returns its element offset (slotIndex * SlotElementCount).
    /// Grows the backing (preserving existing contents) if no free slot is available.</summary>
    int Allocate();

    /// <summary>Returns the writable mapped span for the slot at <paramref name="elementOffset"/>.
    /// Intended to be written once right after <see cref="Allocate"/>.</summary>
    Span<T> GetSpan(int elementOffset);

    /// <summary>Frees a slot previously returned by <see cref="Allocate"/>. The slot is reused only
    /// after in-flight submissions that may still reference it have completed.</summary>
    void Free(int elementOffset);
}
