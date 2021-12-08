using TheMaths;

namespace WDE.MapRenderer.StaticData
{
    public static class Extensions
    {
        public static Vector3 ChunkToWoWPosition(this (int x, int y) chunk)
        {
            return new Vector3((-chunk.x + 32) * Constants.BlockSize, (-chunk.y + 32) * Constants.BlockSize, 0);
        }
        
        public static (int, int) WoWPositionToChunk(this Vector3 wow)
        {
            return ((int)Math.Floor(32 - wow.X / Constants.BlockSize),
                (int)Math.Floor(32 - wow.Y / Constants.BlockSize));
        }

        public static Vector3 ToClientCoords(this Vector3 server)
        {
            float zeropoint = (float)17066.666;
            return new Vector3(zeropoint - server.Y, zeropoint - server.X, server.Z);
        }

    }
}