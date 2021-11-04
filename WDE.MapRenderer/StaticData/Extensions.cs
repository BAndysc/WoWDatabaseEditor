using TheMaths;

namespace WDE.MapRenderer.StaticData
{
    public static class Extensions
    {
        public static Vector3 ToWoWPosition(this Vector3 openGl)
        {
            return new Vector3(-openGl.Z, openGl.X, openGl.Y);
        }
        
        public static Vector3 ToOpenGlPosition(this Vector3 wow)
        {
            return new Vector3(wow.Y, wow.Z, -wow.X);
        }
        
        public static Vector3 ChunkToWoWPosition(this (int x, int y) chunk)
        {
            return new Vector3((-chunk.x + 32) * Constants.BlockSize, (-chunk.y + 32) * Constants.BlockSize, 0);
        }
        
        public static (int, int) WoWPositionToChunk(this Vector3 wow)
        {
            return ((int)Math.Floor(32 - wow.X / Constants.BlockSize),
                (int)Math.Floor(32 - wow.Y / Constants.BlockSize));
        }
    }
}