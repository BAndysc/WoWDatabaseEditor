namespace TheEngine.Interfaces;

/// <summary>
/// A frame-global GPU storage buffer registered at engine init, bound once per frame at
/// set 3. The engine owns N (FramesInFlight) physical backing buffers; BeginWrite returns
/// a span into the CURRENT frame-slot's persistently-mapped memory and must only be called
/// during the render phase (post-BeginFrame fence wait).
/// </summary>
public interface IGlobalBuffer<T> where T : unmanaged
{
    /// <summary>
    /// Returns a span into the current frame-slot's mapped memory sized to hold at least
    /// <paramref name="count"/> elements. Growing reallocs all N backing buffers and
    /// rewrites the set-3 descriptors; capacity only ever increases.
    /// </summary>
    Span<T> BeginWrite(int count);
}
