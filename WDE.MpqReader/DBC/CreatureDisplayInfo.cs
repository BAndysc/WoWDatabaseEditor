using WDE.Common.DBC;
using WDE.Common.MPQ;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader.DBC
{
    public class CreatureDisplayInfo
    {
        public readonly uint Id;
        public readonly int ModelId;
        public readonly int SoundId;
        public readonly uint ExtendedDisplayInfoID;
        public readonly float CreatureModelScale;
        public readonly float CreatureModelAlpha;
        public readonly FileId TextureVariation1;
        public readonly FileId TextureVariation2;
        public readonly FileId TextureVariation3;
        public readonly FileId PortraitTextureName;
        public readonly int BloodID;
        public readonly int NPCSoundID;
        public readonly int ParticleColorID;
        public readonly int CreatureGeosetData;
        public readonly int ObjectEffectPackageID;

        public CreatureDisplayInfo(IDbcIterator dbcIterator, GameFilesVersion version)
        {
            Id = dbcIterator.GetUInt(0);
            ModelId = dbcIterator.GetInt(1);
            SoundId = dbcIterator.GetInt(2);
            ExtendedDisplayInfoID = dbcIterator.GetUInt(3);
            CreatureModelScale = dbcIterator.GetFloat(4);
            CreatureModelAlpha = dbcIterator.GetFloat(5);
            TextureVariation1 = dbcIterator.GetString(6);
            TextureVariation2 = dbcIterator.GetString(7);
            TextureVariation3 = dbcIterator.GetString(8);
            PortraitTextureName = dbcIterator.GetString(9);
            int i = 10;
            if (version == GameFilesVersion.Cataclysm_4_3_4)
                i++; // SizeClass
            BloodID = dbcIterator.GetInt(i++);
            NPCSoundID = dbcIterator.GetInt(i++);
            ParticleColorID = dbcIterator.GetInt(i++);
            CreatureGeosetData = dbcIterator.GetInt(i++);
            ObjectEffectPackageID = dbcIterator.GetInt(i++);
        }
        
        public CreatureDisplayInfo(IWdcIterator dbcIterator, GameFilesVersion version)
        {
            Id = (uint)dbcIterator.Id;

            ModelId = dbcIterator.GetUShort("ModelID");
            SoundId = dbcIterator.GetUShort("NPCSoundID");
            ExtendedDisplayInfoID = (uint)dbcIterator.GetInt("ExtendedDisplayInfoID");
            CreatureModelScale = dbcIterator.GetFloat("CreatureModelScale");
            CreatureModelAlpha = dbcIterator.GetByte("CreatureModelAlpha") / 255.0f;
            TextureVariation1 = dbcIterator.GetInt("TextureVariationFileDataID", 0);
            TextureVariation2 = dbcIterator.GetInt("TextureVariationFileDataID", 1);
            TextureVariation3 = dbcIterator.GetInt("TextureVariationFileDataID", 2);
            PortraitTextureName = dbcIterator.GetInt("PortraitTextureFileDataID");
            BloodID = dbcIterator.GetByte("BloodID");
            NPCSoundID = dbcIterator.GetUShort("NPCSoundID");
            ParticleColorID = dbcIterator.GetUShort("ParticleColorID");
            CreatureGeosetData = dbcIterator.GetInt("CreatureGeosetData");
            ObjectEffectPackageID = dbcIterator.GetUShort("ObjectEffectPackageID");
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
}
