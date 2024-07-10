using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ProtoZeroSharp;

public unsafe partial struct ArenaAllocator
{
    /// <summary>
    /// Single chunk of contiguous memory.
    /// </summary>
    public struct Chunk
    {
        public Chunk* Next;
        public int Used;
        private int length;
        private byte* data; // this pointer shall not be freed, because it is stored directly in the Chunk struct itself

        public int FreeBytes => length - Used;

        public int Length => length;

        public Span<byte> GetSpan() => GetSpan(0);

        public Span<byte> GetSpan(int offset) => GetSpan(offset, Used - offset);

        public Span<byte> GetSpan(int offset, int spanLength)
        {
#if DEBUG
            if (spanLength > Length - offset)
                throw new InvalidOperationException("Requested span is larger than the available data.");
#endif
            return new Span<byte>(data + offset, spanLength);
        }

        /// <summary>
        /// Erases a part of the data in this chunk.
        /// If the erased data is at the end of the chunk, it just reduces the Used counter.
        /// Otherwise, it shifts the data to the left.
        /// </summary>
        public void Erase(int start, int eraseLength)
        {
            // Removing from a chunked array is easy
            // If we are removing from the end, we just reduce the Used counter
            if (start + eraseLength >= Used)
            {
                // if we are exceeding current Chunk, we need to erase the next chunk
                if (start + eraseLength > Used && Next != null)
                    Next->Erase(0, start + eraseLength - Used);
                Used = start;
            }
            else // if we are removing from the middle, we need to shift the data
            {
                var toCopy = Used - (start + eraseLength);
                Span<byte> src = new Span<byte>(data + start + eraseLength, toCopy);
                Span<byte> dst = new Span<byte>(data + start, toCopy);
                src.CopyTo(dst);
                Used -= eraseLength;
            }
        }

        /// <summary>
        /// Allocates a new chunk with the given length using unmanaged memory, that needs to be freed later using
        /// FreeChunk()
        /// </summary>
        public static Chunk* AllocChunk(int length)
        {
            // allocating a chunk with a fixed size, the data is stored directly in the Chunk struct itself
            Chunk* chunk = (Chunk*)Marshal.AllocHGlobal(sizeof(Chunk) + length);
            chunk->Used = 0;
            chunk->Next = null;
            chunk->length = length;
            chunk->data = (byte*)chunk + sizeof(Chunk);
            return chunk;
        }

        /// <summary>
        /// Frees the memory allocated for one chunk.
        /// </summary>
        /// <param name="chunk"></param>
        private static void FreeChunk(Chunk* chunk)
        {
            Marshal.FreeHGlobal((IntPtr)chunk);
        }

        /// <summary>
        /// Frees the memory allocated for this chunk and all the following chunks in the chain.
        /// </summary>
        /// <param name="first"></param>
        public static void FreeChunksChain(Chunk* first)
        {
            while (first != null)
            {
                var next = first->Next;
                FreeChunk(first);
                first = next;
            }
        }
    }

    public struct ChunkOffset
    {
        public readonly Chunk* Chunk;
        public readonly int Offset;

        public ChunkOffset(Chunk* chunk, int offset)
        {
            Chunk = chunk;
            Offset = offset;
        }
    }
}