namespace WDE.MapRenderer.StaticData
{
    public static class Constants
    {
        public const float BlockSize = 533.33333f;
        public const int ChunksInBlockX = 16;
        public const float ChunkSize = BlockSize / ChunksInBlockX;
        public const int ChunksInBlockY = 16;
        public const int ChunksInBlock = ChunksInBlockX * ChunksInBlockY;
        public const int Blocks = 64;
        public const float UnitSize = ChunkSize / 8;
        public const ushort VerticesInChunk = 9 * 9 + 8 * 8;
        public const float MapSize = BlockSize * Blocks;
    }
}