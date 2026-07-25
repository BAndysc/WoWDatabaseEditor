using System.Runtime.CompilerServices;

// The generated bindings pass blittable structs by pointer to native VMA; disable the legacy
// runtime marshaller so the Silk.NET structs are passed straight through.
[assembly: DisableRuntimeMarshalling]

namespace VMASharp;

// Opaque VMA handle wrappers (verbatim from the VMABindings submodule's VMABindings.cs, which
// itself can't be compiled here because it hardcodes `global using OpenTK.Graphics.Vulkan;`).

public partial struct VmaAllocator
{
    public static VmaAllocator Zero => new VmaAllocator(0);
    public nint Handle;
    public VmaAllocator(nint handle) => Handle = handle;
    public override bool Equals(object? obj) => obj is VmaAllocator instance && Equals(instance);
    public bool Equals(VmaAllocator other) => Handle.Equals(other.Handle);
    public override int GetHashCode() => HashCode.Combine(Handle);
    public override string? ToString() => Handle.ToString();
    public static bool operator ==(VmaAllocator left, VmaAllocator right) => left.Equals(right);
    public static bool operator !=(VmaAllocator left, VmaAllocator right) => !(left == right);
    public static explicit operator nint(VmaAllocator handle) => handle.Handle;
    public static explicit operator VmaAllocator(nint handle) => new VmaAllocator(handle);
}

public partial struct VmaPool
{
    public static VmaPool Zero => new VmaPool(0);
    public nint Handle;
    public VmaPool(nint handle) => Handle = handle;
    public override bool Equals(object? obj) => obj is VmaPool instance && Equals(instance);
    public bool Equals(VmaPool other) => Handle.Equals(other.Handle);
    public override int GetHashCode() => HashCode.Combine(Handle);
    public override string? ToString() => Handle.ToString();
    public static bool operator ==(VmaPool left, VmaPool right) => left.Equals(right);
    public static bool operator !=(VmaPool left, VmaPool right) => !(left == right);
    public static explicit operator nint(VmaPool handle) => handle.Handle;
    public static explicit operator VmaPool(nint handle) => new VmaPool(handle);
}

public partial struct VmaAllocation
{
    public static VmaAllocation Zero => new VmaAllocation(0);
    public nint Handle;
    public VmaAllocation(nint handle) => Handle = handle;
    public override bool Equals(object? obj) => obj is VmaAllocation instance && Equals(instance);
    public bool Equals(VmaAllocation other) => Handle.Equals(other.Handle);
    public override int GetHashCode() => HashCode.Combine(Handle);
    public override string? ToString() => Handle.ToString();
    public static bool operator ==(VmaAllocation left, VmaAllocation right) => left.Equals(right);
    public static bool operator !=(VmaAllocation left, VmaAllocation right) => !(left == right);
    public static explicit operator nint(VmaAllocation handle) => handle.Handle;
    public static explicit operator VmaAllocation(nint handle) => new VmaAllocation(handle);
}

public partial struct VmaDefragmentationContext
{
    public static VmaDefragmentationContext Zero => new VmaDefragmentationContext(0);
    public nint Handle;
    public VmaDefragmentationContext(nint handle) => Handle = handle;
    public override bool Equals(object? obj) => obj is VmaDefragmentationContext instance && Equals(instance);
    public bool Equals(VmaDefragmentationContext other) => Handle.Equals(other.Handle);
    public override int GetHashCode() => HashCode.Combine(Handle);
    public override string? ToString() => Handle.ToString();
    public static bool operator ==(VmaDefragmentationContext left, VmaDefragmentationContext right) => left.Equals(right);
    public static bool operator !=(VmaDefragmentationContext left, VmaDefragmentationContext right) => !(left == right);
    public static explicit operator nint(VmaDefragmentationContext handle) => handle.Handle;
    public static explicit operator VmaDefragmentationContext(nint handle) => new VmaDefragmentationContext(handle);
}

public partial struct VmaVirtualAllocation
{
    public static VmaVirtualAllocation Zero => new VmaVirtualAllocation(0);
    public nint Handle;
    public VmaVirtualAllocation(nint handle) => Handle = handle;
    public override bool Equals(object? obj) => obj is VmaVirtualAllocation instance && Equals(instance);
    public bool Equals(VmaVirtualAllocation other) => Handle.Equals(other.Handle);
    public override int GetHashCode() => HashCode.Combine(Handle);
    public override string? ToString() => Handle.ToString();
    public static bool operator ==(VmaVirtualAllocation left, VmaVirtualAllocation right) => left.Equals(right);
    public static bool operator !=(VmaVirtualAllocation left, VmaVirtualAllocation right) => !(left == right);
    public static explicit operator nint(VmaVirtualAllocation handle) => handle.Handle;
    public static explicit operator VmaVirtualAllocation(nint handle) => new VmaVirtualAllocation(handle);
}

public partial struct VmaVirtualBlock
{
    public static VmaVirtualBlock Zero => new VmaVirtualBlock(0);
    public nint Handle;
    public VmaVirtualBlock(nint handle) => Handle = handle;
    public override bool Equals(object? obj) => obj is VmaVirtualBlock instance && Equals(instance);
    public bool Equals(VmaVirtualBlock other) => Handle.Equals(other.Handle);
    public override int GetHashCode() => HashCode.Combine(Handle);
    public override string? ToString() => Handle.ToString();
    public static bool operator ==(VmaVirtualBlock left, VmaVirtualBlock right) => left.Equals(right);
    public static bool operator !=(VmaVirtualBlock left, VmaVirtualBlock right) => !(left == right);
    public static explicit operator nint(VmaVirtualBlock handle) => handle.Handle;
    public static explicit operator VmaVirtualBlock(nint handle) => new VmaVirtualBlock(handle);
}
