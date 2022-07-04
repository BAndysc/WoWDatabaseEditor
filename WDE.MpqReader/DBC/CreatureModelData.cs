using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader.DBC;

public class CreatureModelData
{
    public readonly uint Id;
//    public readonly int Flags;
    public readonly FileId ModelName;
    // public readonly int SizeClass;
    // public readonly float ModelScale;
    // public readonly int BloodID;
    // public readonly int FootprintTextureID;
    // public readonly float FootprintTextureLength;
    // public readonly float FootprintTextureWidth;
    // public readonly float FootprintParticleScale;
    // public readonly int FoleyMaterialID;
    // public readonly int FootstepShakeSize;
    // public readonly int DeathThudShakeSize;
    // public readonly int SoundID;
    // public readonly float CollisionWidth;
    // public readonly float CollisionHeight;
    // public readonly float MountHeight;
    // public readonly float GeoBoxMinX;
    // public readonly float GeoBoxMinY;
    // public readonly float GeoBoxMinZ;
    // public readonly float GeoBoxMaxX;
    // public readonly float GeoBoxMaxY;
    // public readonly float GeoBoxMaxZ;
    // public readonly float WorldEffectScale;
    // public readonly float AttachedEffectScale;
    // public readonly float MissileCollisionRadius;
    // public readonly float MissileCollisionPush;
    // public readonly float MissileCollisionRaise;

    public CreatureModelData(IDbcIterator dbcIterator)
    {
        Id = dbcIterator.GetUInt(0);
//        Flags = dbcIterator.GetInt(1);
        ModelName = dbcIterator.GetString(2);
        // SizeClass = dbcIterator.GetInt(3);
        // ModelScale = dbcIterator.GetFloat(4);
        // BloodID = dbcIterator.GetInt(5);
        // FootprintTextureID = dbcIterator.GetInt(6);
        // FootprintTextureLength = dbcIterator.GetFloat(7);
        // FootprintTextureWidth = dbcIterator.GetFloat(8);
        // FootprintParticleScale = dbcIterator.GetFloat(9);
        // FoleyMaterialID = dbcIterator.GetInt(10);
        // FootstepShakeSize = dbcIterator.GetInt(11);
        // DeathThudShakeSize = dbcIterator.GetInt(12);
        // SoundID = dbcIterator.GetInt(13);
        // CollisionWidth = dbcIterator.GetFloat(14);
        // CollisionHeight = dbcIterator.GetFloat(15);
        // MountHeight = dbcIterator.GetFloat(16);
        // GeoBoxMinX = dbcIterator.GetFloat(17);
        // GeoBoxMinY = dbcIterator.GetFloat(18);
        // GeoBoxMinZ = dbcIterator.GetFloat(19);
        // GeoBoxMaxX = dbcIterator.GetFloat(20);
        // GeoBoxMaxY = dbcIterator.GetFloat(21);
        // GeoBoxMaxZ = dbcIterator.GetFloat(22);
        // WorldEffectScale = dbcIterator.GetFloat(23);
        // AttachedEffectScale = dbcIterator.GetFloat(24);
        // MissileCollisionRadius = dbcIterator.GetFloat(25);
        // MissileCollisionPush = dbcIterator.GetFloat(26);
        // MissileCollisionRaise = dbcIterator.GetFloat(27);
    }

    public CreatureModelData(IWdcIterator dbcIterator)
    {
        Id = (uint)dbcIterator.Id;
        ModelName = dbcIterator.GetUInt("FileDataID");
    }

    private CreatureModelData()
    {
        Id = 0;
        //Flags = 0;
        ModelName = "";
        // SizeClass = 0;
        // ModelScale = 0;
        // BloodID = 0;
        // FootprintTextureID = 0;
        // FootprintTextureLength = 0;
        // FootprintTextureWidth = 0;
        // FootprintParticleScale = 0;
        // FoleyMaterialID = 0;
        // FootstepShakeSize = 0;
        // DeathThudShakeSize = 0;
        // SoundID = 0;
        // CollisionWidth = 0;
        // CollisionHeight = 0;
        // MountHeight = 0;
        // GeoBoxMinX = 0;
        // GeoBoxMinY = 0;
        // GeoBoxMinZ = 0;
        // GeoBoxMaxX = 0;
        // GeoBoxMaxY = 0;
        // GeoBoxMaxZ = 0;
        // WorldEffectScale = 0;
        // AttachedEffectScale = 0;
        // MissileCollisionRadius = 0;
        // MissileCollisionPush = 0;
        // MissileCollisionRaise = 0;
    }

    public static CreatureModelData Empty => new CreatureModelData();
}