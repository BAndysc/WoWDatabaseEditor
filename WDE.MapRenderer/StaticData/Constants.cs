namespace WDE.MapRenderer.StaticData
{
    public static class Constants
    {
        public static float BlockSize = 533.33333f;
        public static int ChunksInBlockX = 16;
        public static float ChunkSize = BlockSize / ChunksInBlockX;
        public static int ChunksInBlockY = 16;
        public static int ChunksInBlock = ChunksInBlockX * ChunksInBlockY;
        public static int Blocks = 64;
        public static ushort VerticesInChunk = 9 * 9 + 8 * 8;
    }
}