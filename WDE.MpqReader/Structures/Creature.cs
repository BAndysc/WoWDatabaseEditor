using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.Structures
{
    public class CreatureDisplayInfo
    {
        public readonly uint Id;
        public readonly int ModelId;
        public readonly int SoundId;
        public readonly int ExtendedDisplayInfoID;
        public readonly float CreatureModelScale;
        public readonly float CreatureModelAlpha;
        public readonly string TextureVariation1;
        public readonly string TextureVariation2;
        public readonly string TextureVariation3;
        public readonly string PortraitTextureName;
        public readonly int BloodID;
        public readonly int NPCSoundID;
        public readonly int ParticleColorID;
        public readonly int CreatureGeosetData;
        public readonly int ObjectEffectPackageID;

        public CreatureDisplayInfo(IDbcIterator dbcIterator)
        {
            Id = dbcIterator.GetUInt(0);
            ModelId = dbcIterator.GetInt(1);
            SoundId = dbcIterator.GetInt(2);
            ExtendedDisplayInfoID = dbcIterator.GetInt(3);
            CreatureModelScale = dbcIterator.GetFloat(4);
            CreatureModelAlpha = dbcIterator.GetFloat(5);
            TextureVariation1 = dbcIterator.GetString(6);
            TextureVariation2 = dbcIterator.GetString(7);
            TextureVariation3 = dbcIterator.GetString(8);
            PortraitTextureName = dbcIterator.GetString(9);
            BloodID = dbcIterator.GetInt(10);
            NPCSoundID = dbcIterator.GetInt(11);
            ParticleColorID = dbcIterator.GetInt(12);
            CreatureGeosetData = dbcIterator.GetInt(13);
            ObjectEffectPackageID = dbcIterator.GetInt(14);

        }

        private CreatureDisplayInfo()
        {
            Id = 0;
            ModelId = 0;
            SoundId = 0;
            ExtendedDisplayInfoID = 0;
            CreatureModelScale = 0;
            CreatureModelAlpha = 0;
            TextureVariation1 = "";
            TextureVariation2 = "";
            TextureVariation3 = "";
            PortraitTextureName = "";
            BloodID = 0;
            NPCSoundID = 0;
            ParticleColorID = 0;
            CreatureGeosetData = 0;
            ObjectEffectPackageID = 0;
        }

        public static CreatureDisplayInfo Empty => new CreatureDisplayInfo();
    }

    public class CreatureDisplayInfoStore : IEnumerable<CreatureDisplayInfo>
    {
        private Dictionary<uint, CreatureDisplayInfo> store = new();
        public CreatureDisplayInfoStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new CreatureDisplayInfo(row);
                store[o.Id] = o;
            }
        }

        public bool Contains(uint id) => store.ContainsKey(id);
        public CreatureDisplayInfo this[uint id] => store[id];
        public IEnumerator<CreatureDisplayInfo> GetEnumerator() => store.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
    }

    public class CreatureModelData
    {
        public readonly int Id;
        public readonly int Flags;
        public readonly string ModelName;
        public readonly int SizeClass;
        public readonly float ModelScale;
        public readonly int BloodID;
        public readonly int FootprintTextureID;
        public readonly float FootprintTextureLength;
        public readonly float FootprintTextureWidth;
        public readonly float FootprintParticleScale;
        public readonly int FoleyMaterialID;
        public readonly int FootstepShakeSize;
        public readonly int DeathThudShakeSize;
        public readonly int SoundID;
        public readonly float CollisionWidth;
        public readonly float CollisionHeight;
        public readonly float MountHeight;
        public readonly float GeoBoxMinX;
        public readonly float GeoBoxMinY;
        public readonly float GeoBoxMinZ;
        public readonly float GeoBoxMaxX;
        public readonly float GeoBoxMaxY;
        public readonly float GeoBoxMaxZ;
        public readonly float WorldEffectScale;
        public readonly float AttachedEffectScale;
        public readonly float MissileCollisionRadius;
        public readonly float MissileCollisionPush;
        public readonly float MissileCollisionRaise;

        public CreatureModelData(IDbcIterator dbcIterator)
        {
            Id = dbcIterator.GetInt(0);
            Flags = dbcIterator.GetInt(1);
            ModelName = dbcIterator.GetString(2);
            SizeClass = dbcIterator.GetInt(3);
            ModelScale = dbcIterator.GetFloat(4);
            BloodID = dbcIterator.GetInt(5);
            FootprintTextureID = dbcIterator.GetInt(6);
            FootprintTextureLength = dbcIterator.GetFloat(7);
            FootprintTextureWidth = dbcIterator.GetFloat(8);
            FootprintParticleScale = dbcIterator.GetFloat(9);
            FoleyMaterialID = dbcIterator.GetInt(10);
            FootstepShakeSize = dbcIterator.GetInt(11);
            DeathThudShakeSize = dbcIterator.GetInt(12);
            SoundID = dbcIterator.GetInt(13);
            CollisionWidth = dbcIterator.GetFloat(14);
            CollisionHeight = dbcIterator.GetFloat(15);
            MountHeight = dbcIterator.GetFloat(16);
            GeoBoxMinX = dbcIterator.GetFloat(17);
            GeoBoxMinY = dbcIterator.GetFloat(18);
            GeoBoxMinZ = dbcIterator.GetFloat(19);
            GeoBoxMaxX = dbcIterator.GetFloat(20);
            GeoBoxMaxY = dbcIterator.GetFloat(21);
            GeoBoxMaxZ = dbcIterator.GetFloat(22);
            WorldEffectScale = dbcIterator.GetFloat(23);
            AttachedEffectScale = dbcIterator.GetFloat(24);
            MissileCollisionRadius = dbcIterator.GetFloat(25);
            MissileCollisionPush = dbcIterator.GetFloat(26);
            MissileCollisionRaise = dbcIterator.GetFloat(27);
        }

        private CreatureModelData()
        {
            Id = -1;
            Flags = 0;
            ModelName = "";
            SizeClass = 0;
            ModelScale = 0;
            BloodID = 0;
            FootprintTextureID = 0;
            FootprintTextureLength = 0;
            FootprintTextureWidth = 0;
            FootprintParticleScale = 0;
            FoleyMaterialID = 0;
            FootstepShakeSize = 0;
            DeathThudShakeSize = 0;
            SoundID = 0;
            CollisionWidth = 0;
            CollisionHeight = 0;
            MountHeight = 0;
            GeoBoxMinX = 0;
            GeoBoxMinY = 0;
            GeoBoxMinZ = 0;
            GeoBoxMaxX = 0;
            GeoBoxMaxY = 0;
            GeoBoxMaxZ = 0;
            WorldEffectScale = 0;
            AttachedEffectScale = 0;
            MissileCollisionRadius = 0;
            MissileCollisionPush = 0;
            MissileCollisionRaise = 0;
        }

        public static CreatureModelData Empty => new CreatureModelData();
    }

    public class CreatureModelDataStore : IEnumerable<CreatureModelData>
    {
        private Dictionary<int, CreatureModelData> store = new();
        public CreatureModelDataStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new CreatureModelData(row);
                store[o.Id] = o;
            }
        }

        public bool Contains(int id) => store.ContainsKey(id);
        public CreatureModelData this[int id] => store[id];
        public IEnumerator<CreatureModelData> GetEnumerator() => store.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
    }
}
