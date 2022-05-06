using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.Structures
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

    public abstract class BaseDbcStore<TKey, TVal> : IEnumerable<TVal> where TKey : notnull
    {
        protected Dictionary<TKey, TVal> store = new();
        public bool TryGetValue(TKey id, out TVal val) => store.TryGetValue(id, out val!);
        public bool Contains(TKey id) => store.ContainsKey(id);
        public TVal this[TKey id] => store[id];
        public IEnumerator<TVal> GetEnumerator() => store.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
    }

    public class CreatureDisplayInfoStore : BaseDbcStore<uint, CreatureDisplayInfo>
    {
        public CreatureDisplayInfoStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new CreatureDisplayInfo(row);
                store[o.Id] = o;
            }
        }
    }

    public class CreatureModelData
    {
        public readonly uint Id;
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
            Id = dbcIterator.GetUInt(0);
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
            Id = 0;
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

    public class CreatureModelDataStore : BaseDbcStore<uint, CreatureModelData>
    {
        public CreatureModelDataStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new CreatureModelData(row);
                store[o.Id] = o;
            }
        }
    }

    public enum EmoteType
    {
        OneShotEmote,
        ChangeStandState,
        SetEmoteState
    }
    
    public class Emote
    {
        public readonly uint Id;
        public readonly string Name;
        public readonly uint AnimId;
        public readonly uint Flags;
        public readonly EmoteType Type;
        public readonly uint Param;
        public readonly uint Sound;

        public Emote(IDbcIterator iterator)
        {
            Id = iterator.GetUInt(0);
            Name = iterator.GetString(1);
            AnimId = iterator.GetUInt(2);
            Flags = iterator.GetUInt(3);
            Type = (EmoteType)iterator.GetUInt(4);
            Param = iterator.GetUInt(5);
            Sound = iterator.GetUInt(6);
        }
    }
    
    public class CreatureDisplayInfoExtra
    {
        public readonly uint Id;
        public readonly int Race;
        public readonly int CreatureType;
        public readonly int Gender;
        public readonly int SkinColor;
        public readonly int FaceType;
        public readonly int HairStyle;
        public readonly int HairColor;
        public readonly int BeardStyle;
        public readonly int Helm;
        public readonly int Shoulder;
        public readonly int Shirt;
        public readonly int Cuirass;
        public readonly int Belt;
        public readonly int Legs;
        public readonly int Boots;
        public readonly int Wrist;
        public readonly int Gloves;
        public readonly int Tabard;
        public readonly int Cape;
        public readonly int Flags;
        public readonly string Texture;


        public CreatureDisplayInfoExtra(IDbcIterator dbcIterator)
        {
            Id = dbcIterator.GetUInt(0);
            Race = dbcIterator.GetInt(1);
            // CreatureType = dbcIterator.GetInt(2);
            Gender = dbcIterator.GetInt(2);
            SkinColor = dbcIterator.GetInt(3);
            FaceType = dbcIterator.GetInt(4);
            HairStyle = dbcIterator.GetInt(5);
            HairColor = dbcIterator.GetInt(6);
            BeardStyle = dbcIterator.GetInt(7);
            Helm = dbcIterator.GetInt(8);
            Shoulder = dbcIterator.GetInt(9);
            Shirt = dbcIterator.GetInt(10);
            Cuirass = dbcIterator.GetInt(11);
            Belt = dbcIterator.GetInt(12);
            Legs = dbcIterator.GetInt(13);
            Boots = dbcIterator.GetInt(14);
            Wrist = dbcIterator.GetInt(15);
            Gloves = dbcIterator.GetInt(16);
            Tabard = dbcIterator.GetInt(17);
            Cape = dbcIterator.GetInt(18);
            Flags = dbcIterator.GetInt(19);
            Texture = dbcIterator.GetString(20);
        }

        private CreatureDisplayInfoExtra()
        {
            Id = 0;
            Race = 0;
            CreatureType = 0;
            Gender = 0;
            SkinColor = 0;
            FaceType = 0;
            HairStyle = 0;
            HairColor = 0;
            BeardStyle = 0;
            Helm = 0;
            Shoulder = 0;
            Shirt = 0;
            Cuirass = 0;
            Belt = 0;
            Legs = 0;
            Boots = 0;
            Wrist = 0;
            Gloves = 0;
            Tabard = 0;
            Cape = 0;
            Flags = 0;
            Texture = "";
        }

        public static CreatureDisplayInfoExtra Empty => new CreatureDisplayInfoExtra();
    }

    public class CreatureDisplayInfoExtraStore : BaseDbcStore<uint, CreatureDisplayInfoExtra>
    {
        public CreatureDisplayInfoExtraStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new CreatureDisplayInfoExtra(row);
                store[o.Id] = o;
            }
        }
    }

    public class EmoteStore : BaseDbcStore<uint, Emote>
    {
        public EmoteStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new Emote(row);
                store[o.Id] = o;
            }
        }
    }

    public class ItemDisplayInfo
    {
        public readonly uint Id;
        public readonly string LeftModel;
        public readonly string RightModel;
        public readonly string LeftModelTexture;
        public readonly string RightModelTexture;
        public readonly string Icon1;
        public readonly string Icon2;
        public readonly int geosetGroup1;
        public readonly int geosetGroup2;
        public readonly int geosetGroup3;
        public readonly int Flags;
        public readonly int SpellVisualID;
        public readonly int groupSoundIndex;
        public readonly int helmetGeosetVisMale;
        public readonly int helmetGeosetVisFemale;
        public readonly string UpperArmTexture;
        public readonly string LowerArmTexture;
        public readonly string HandsTexture;
        public readonly string UpperTorsoTexture;
        public readonly string LowerTorsoTexture;
        public readonly string UpperLegTexture;
        public readonly string LowerLegTexture;
        public readonly string FootTexture;
        public readonly int itemVisual;
        public readonly int particleColorID;


        public ItemDisplayInfo(IDbcIterator dbcIterator)
        {
            Id = dbcIterator.GetUInt(0);
            LeftModel = dbcIterator.GetString(1);
            RightModel = dbcIterator.GetString(2);
            LeftModelTexture = dbcIterator.GetString(3);
            RightModelTexture = dbcIterator.GetString(4);
            Icon1 = dbcIterator.GetString(5);
            Icon2 = dbcIterator.GetString(6);
            geosetGroup1 = dbcIterator.GetInt(7);
            geosetGroup2 = dbcIterator.GetInt(8);
            geosetGroup3 = dbcIterator.GetInt(9);
            Flags = dbcIterator.GetInt(10);
            SpellVisualID = dbcIterator.GetInt(11);
            groupSoundIndex = dbcIterator.GetInt(12);
            helmetGeosetVisMale = dbcIterator.GetInt(13);
            helmetGeosetVisFemale = dbcIterator.GetInt(14);
            UpperArmTexture = dbcIterator.GetString(15);
            LowerArmTexture = dbcIterator.GetString(16);
            HandsTexture = dbcIterator.GetString(17);
            UpperTorsoTexture = dbcIterator.GetString(18);
            LowerTorsoTexture = dbcIterator.GetString(19);
            UpperLegTexture = dbcIterator.GetString(20);
            LowerLegTexture = dbcIterator.GetString(21);
            FootTexture = dbcIterator.GetString(22);
            itemVisual = dbcIterator.GetInt(23);
            particleColorID = dbcIterator.GetInt(24);
        }

        private ItemDisplayInfo()
        {
            Id = 0;
            LeftModel = "";
            RightModel = "";
            LeftModelTexture = "";
            RightModelTexture = "";
            Icon1 = "";
            Icon2 = "";
            geosetGroup1 = 0;
            geosetGroup2 = 0;
            geosetGroup3 = 0;
            Flags = 0;
            SpellVisualID = 0;
            groupSoundIndex = 0;
            helmetGeosetVisMale = 0;
            helmetGeosetVisFemale = 0;
            UpperArmTexture = "";
            LowerArmTexture = "";
            HandsTexture = "";
            UpperTorsoTexture = "";
            LowerTorsoTexture = "";
            UpperLegTexture = "";
            LowerLegTexture = "";
            FootTexture = "";
            itemVisual = 0;
            particleColorID = 0;
        }

        public static ItemDisplayInfo Empty => new ItemDisplayInfo();
    }

    public class ItemDisplayInfoStore : IEnumerable<ItemDisplayInfo>
    {
        private Dictionary<uint, ItemDisplayInfo> store = new();
        public ItemDisplayInfoStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new ItemDisplayInfo(row);
                store[o.Id] = o;
            }
        }

        public bool Contains(uint id) => store.ContainsKey(id);
        public ItemDisplayInfo this[uint id] => store[id];
        public IEnumerator<ItemDisplayInfo> GetEnumerator() => store.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
    }

    public class CharSections
    {
        public readonly uint Id;
        public readonly int RaceID;
        public readonly int SexId;
        public readonly int BaseSection;
        public readonly string TextureName1;
        public readonly string TextureName2;
        public readonly string TextureName3;
        public readonly int Flags;
        public readonly int VariationIndex;
        public readonly int ColorIndex;

        public CharSections(IDbcIterator dbcIterator)
        {
            Id = dbcIterator.GetUInt(0);
            RaceID = dbcIterator.GetInt(1);
            SexId = dbcIterator.GetInt(2);
            BaseSection = dbcIterator.GetInt(3);
            TextureName1 = dbcIterator.GetString(4);
            TextureName2 = dbcIterator.GetString(5);
            TextureName3 = dbcIterator.GetString(6);
            Flags = dbcIterator.GetInt(7);
            VariationIndex = dbcIterator.GetInt(8);
            ColorIndex = dbcIterator.GetInt(9);
        }

        private CharSections()
        {
            Id = 0;
            RaceID = 0;
            SexId = 0;
            BaseSection = 0;
            TextureName1 = "";
            TextureName2 = "";
            TextureName3 = "";
            Flags = 0;
            VariationIndex = 0;
            ColorIndex = 0;
        }

        public static CharSections Empty => new CharSections();
    }

    public class CharSectionsStore : IEnumerable<CharSections>
    {
        private Dictionary<uint, CharSections> store = new();
        public CharSectionsStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new CharSections(row);
                store[o.Id] = o;
            }
        }

        public bool Contains(uint id) => store.ContainsKey(id);
        public CharSections this[uint id] => store[id];
        public IEnumerator<CharSections> GetEnumerator() => store.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
    }

    public class ChrRaces
    {
        public readonly uint Id;
        public readonly int Flags;
        public readonly int FactionID;
        public readonly int ExplorationSoundID;
        public readonly int MaleDisplayId;
        public readonly int FemaleDisplayId;
        public readonly string ClientPrefix;
        public readonly int BaseLanguage;
        public readonly int CreatureType;
        public readonly int ResSicknessSpellID;
        public readonly int SplashSoundID;
        public readonly string ClientFilestring;
        public readonly int CinematicSequenceID;
        public readonly int Alliance;
        public readonly string Name_Lang;
        public readonly string Name_Female_Lang;
        public readonly string Name_Male_Lang;
        public readonly string FacialHairCustomization1;
        public readonly string FacialHairCustomization2;
        public readonly string HairCustomization;
        public readonly int Required_Expansion;

        public ChrRaces(IDbcIterator dbcIterator)
        {
            Id = dbcIterator.GetUInt(0);
            Flags = dbcIterator.GetInt(1);
            FactionID = dbcIterator.GetInt(2);
            ExplorationSoundID = dbcIterator.GetInt(3);
            MaleDisplayId = dbcIterator.GetInt(4);
            FemaleDisplayId = dbcIterator.GetInt(5);
            ClientPrefix = dbcIterator.GetString(6);
            BaseLanguage = dbcIterator.GetInt(7);
            CreatureType = dbcIterator.GetInt(8);
            ResSicknessSpellID = dbcIterator.GetInt(9);
            SplashSoundID = dbcIterator.GetInt(10);
            ClientFilestring = dbcIterator.GetString(11);
            // CinematicSequenceID = dbcIterator.GetInt(12);
            // Alliance = dbcIterator.GetInt(13);
            // TODO : Name_Lang = dbcIterator.GetString(1);
            // TODO : Name_Female_Lang = dbcIterator.GetString(1);
            // TODO : Name_Male_Lang = dbcIterator.GetString(1);
            // TODO : FacialHairCustomization1 = dbcIterator.GetString(1);
            // TODO : FacialHairCustomization2 = dbcIterator.GetString(1);
            // TODO : HairCustomization = dbcIterator.GetString(1);
            // TODO : Required_Expansion = dbcIterator.GetInt(1);
        }

        private ChrRaces()
        {
            Id = 0;
            Flags = 0;
            FactionID = 0;
            ExplorationSoundID = 0;
            MaleDisplayId = 0;
            FemaleDisplayId = 0;
            ClientPrefix = "";
            BaseLanguage = 0;
            CreatureType = 0;
            ResSicknessSpellID = 0;
            SplashSoundID = 0;
            ClientFilestring = "";
            CinematicSequenceID = 0;
            Alliance = 0;
            Name_Lang = "";
            Name_Female_Lang = "";
            Name_Male_Lang = "";
            FacialHairCustomization1 = "";
            FacialHairCustomization2 = "";
            HairCustomization = "";
            Required_Expansion = 0;
        }

        public static ChrRaces Empty => new ChrRaces();
    }

    public class ChrRacesStore : IEnumerable<ChrRaces>
    {
        private Dictionary<uint, ChrRaces> store = new();
        public ChrRacesStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new ChrRaces(row);
                store[o.Id] = o;
            }
        }

        public bool Contains(uint id) => store.ContainsKey(id);
        public ChrRaces this[uint id] => store[id];
        public IEnumerator<ChrRaces> GetEnumerator() => store.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
    }

    public class CharacterFacialHairStyles
    {
        // public readonly uint Id; // deosn't exist in dbc
        public readonly int RaceID;
        public readonly int SexId;
        public readonly int VariationId;
        public readonly int Geoset1;
        public readonly int Geoset2;
        public readonly int Geoset3;
        public readonly int Geoset4;
        public readonly int Geoset5;

        public CharacterFacialHairStyles(IDbcIterator dbcIterator)
        {
            // Id = 0;
            RaceID = dbcIterator.GetInt(0);
            SexId = dbcIterator.GetInt(1);
            VariationId = dbcIterator.GetInt(2);
            Geoset1 = dbcIterator.GetInt(3);
            Geoset2 = dbcIterator.GetInt(4);
            Geoset3 = dbcIterator.GetInt(5);
            Geoset4 = dbcIterator.GetInt(6);
            Geoset5 = dbcIterator.GetInt(7);
        }

        private CharacterFacialHairStyles()
        {
            // Id = 0;
            RaceID = 0;
            SexId = 0;
            VariationId = 0;
            Geoset1 = 0;
            Geoset2 = 0;
            Geoset3 = 0;
            Geoset4 = 0;
            Geoset5 = 0;
        }

        public static CharacterFacialHairStyles Empty => new CharacterFacialHairStyles();
    }

    public class CharacterFacialHairStylesStore : IEnumerable<CharacterFacialHairStyles>
    {
        private Dictionary<uint, CharacterFacialHairStyles> store = new();
        public CharacterFacialHairStylesStore(IEnumerable<IDbcIterator> rows)
        {
            uint idx = 1;
            foreach (var row in rows)
            {
                var o = new CharacterFacialHairStyles(row);
                store[idx] = o;

                idx++;
            }
        }

        // public bool Contains(uint id) => store.ContainsKey(id);
        // public CharacterFacialHairStyles this[uint id] => store[id];
        public IEnumerator<CharacterFacialHairStyles> GetEnumerator() => store.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
    }

    public class CharHairGeosets
    {
        public readonly uint Id;
        public readonly int RaceID;
        public readonly int SexId;
        public readonly int VariationId;
        public readonly int GeosetId;
        public readonly int ShowScalp; // bald

        public CharHairGeosets(IDbcIterator dbcIterator)
        {
            Id = dbcIterator.GetUInt(0);
            RaceID = dbcIterator.GetInt(1);
            SexId = dbcIterator.GetInt(2);
            VariationId = dbcIterator.GetInt(3);
            GeosetId = dbcIterator.GetInt(4);
            ShowScalp = dbcIterator.GetInt(5);
        }

        private CharHairGeosets()
        {
            Id = 0;
            RaceID = 0;
            SexId = 0;
            VariationId = 0;
            GeosetId = 0;
            ShowScalp = 0;
        }

        public static CharHairGeosets Empty => new CharHairGeosets();
    }

    public class CharHairGeosetsStore : IEnumerable<CharHairGeosets>
    {
        private Dictionary<uint, CharHairGeosets> store = new();
        public CharHairGeosetsStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new CharHairGeosets(row);
                store[o.Id] = o;
            }
        }

        public bool Contains(uint id) => store.ContainsKey(id);
        public CharHairGeosets this[uint id] => store[id];
        public IEnumerator<CharHairGeosets> GetEnumerator() => store.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
    }
}
