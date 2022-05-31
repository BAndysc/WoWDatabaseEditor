using WDE.Common.DBC;

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
            ExtendedDisplayInfoID = dbcIterator.GetUInt(3);
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
}
