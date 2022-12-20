using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader.DBC
{
    public class GameObjectDisplayInfo
    {
        public readonly uint Id;
        public readonly FileId ModelName;
        // public readonly int Sound;
        // public readonly float GeoBoxMinX;
        // public readonly float GeoBoxMinY;
        // public readonly float GeoBoxMinZ;
        // public readonly float GeoBoxMaxX;
        // public readonly float GeoBoxMaxY;
        // public readonly float GeoBoxMaxZ;
        // public readonly int ObjectEffectPackageID;

        public GameObjectDisplayInfo(IDbcIterator dbcIterator)
        {
            Id = dbcIterator.GetUInt(0);
            ModelName = dbcIterator.GetString(1);
            // Sound = dbcIterator.GetInt(2);
            // GeoBoxMinX = dbcIterator.GetFloat(3);
            // GeoBoxMinY = dbcIterator.GetFloat(4);
            // GeoBoxMinZ = dbcIterator.GetFloat(5);
            // GeoBoxMaxX = dbcIterator.GetFloat(6);
            // GeoBoxMaxY = dbcIterator.GetFloat(7);
            // GeoBoxMaxZ = dbcIterator.GetFloat(8);
            // ObjectEffectPackageID = dbcIterator.GetInt(9);
        }
        
        public GameObjectDisplayInfo(IWdcIterator dbcIterator)
        {
            Id = (uint)dbcIterator.Id;
            ModelName = dbcIterator.GetUInt("FileDataID");
        }

        private GameObjectDisplayInfo()
        {
            Id = 0;
            ModelName = "";
            // Sound = -1;
            // GeoBoxMinX = 0;
            // GeoBoxMinY = 0;
            // GeoBoxMinZ = 0;
            // GeoBoxMaxX = 0;
            // GeoBoxMaxY = 0;
            // GeoBoxMaxZ = 0;
            // ObjectEffectPackageID = 0;
        }

        public static GameObjectDisplayInfo Empty => new GameObjectDisplayInfo();
    }
}