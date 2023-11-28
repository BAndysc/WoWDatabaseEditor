using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WDE.Common.DBC;
using WDE.Common.MPQ;
using WDE.Common.Utils;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class DbcManager
    {
        private readonly IGameFiles gameFiles;
        private readonly IDatabaseClientFileOpener opener;

        public AreaTableStore AreaTableStore { get; }
        public AreaTriggerStore AreaTriggerStore { get; }
        public CreatureDisplayInfoStore CreatureDisplayInfoStore { get; }
        public CreatureDisplayInfoExtraStore CreatureDisplayInfoExtraStore { get; }
        public EmoteStore EmoteStore { get; }
        public AnimationDataStore AnimationDataStore { get; }
        public ItemAppearanceStore ItemAppearanceStore { get; }
        public ItemModifiedAppearanceStore ItemModifiedAppearanceStore { get; }
        public ItemStore ItemStore { get; }
        public CurrencyTypeStore CurrencyTypeStore { get; }
        public ItemDisplayInfoStore ItemDisplayInfoStore { get; }
        public CreatureModelDataStore CreatureModelDataStore { get; }
        public GameObjectDisplayInfoStore GameObjectDisplayInfoStore { get; }
        public HelmetGeosetVisDataStore HelmetGeosetVisDataStore { get; }
        public CharSectionsStore CharSectionsStore { get; }
        public ChrRacesStore ChrRacesStore { get; }
        public CharacterFacialHairStylesStore CharacterFacialHairStylesStore { get; }
        public CharHairGeosetsStore CharHairGeosetsStore { get; }
        public MapStore MapStore { get; }
        public LightDataStore LightDataStore { get; }
        public LightIntParamStore LightIntParamStore { get; }
        public LightFloatParamStore LightFloatParamStore { get; }
        public LightParamStore LightParamStore { get; }
        public LightStore LightStore { get; }
        public TextureFileDataStore TextureFileDataStore { get; }
        public ModelFileDataStore ModelFileDataStore { get; }
        public LiquidTypeStore LiquidTypeStore { get; }
        public LiquidObjectStore LiquidObjectStore { get; }
        public LiquidMaterialStore LiquidMaterialStore { get; }
        public WorldMapAreaStore WorldMapAreaStore { get; }

        private IEnumerable<object> OpenDbc(string name)
        {
            var bytes = gameFiles.ReadFileSync($"DBFilesClient\\{name}.dbc");
            if (bytes == null)
                bytes = gameFiles.ReadFileSync($"DBFilesClient\\{name}.db2");

            if (bytes == null)
                throw new Exception($"Couldn't find {name} DBC!");

            if (gameFiles.WoWVersion == GameFilesVersion.Legion_7_3_5)
            {
                try
                {
                    return opener.OpenWdc(name, bytes);
                }
                catch (Exception e)
                {
                    throw new Exception("An exception occured while opening file " + name + " (" + e.Message + ")", e);
                }
            }
            
            return opener.Open(bytes);
        }

        public DbcManager(IGameFiles gameFiles, IDatabaseClientFileOpener opener)
        {
            this.gameFiles = gameFiles;
            this.opener = opener;
            AreaTableStore = new((dynamic)OpenDbc("AreaTable"), gameFiles.WoWVersion);
            AreaTriggerStore = new((dynamic)OpenDbc("AreaTrigger"), gameFiles.WoWVersion);
            CreatureDisplayInfoStore = new((dynamic)OpenDbc("CreatureDisplayInfo"), gameFiles.WoWVersion);
            CreatureDisplayInfoExtraStore = new((dynamic)OpenDbc("CreatureDisplayInfoExtra")); // for humanoids
            CreatureModelDataStore = new((dynamic)OpenDbc("CreatureModelData"));
            EmoteStore = new((dynamic)OpenDbc("Emotes"));
            AnimationDataStore = new((dynamic)OpenDbc("AnimationData"), gameFiles.WoWVersion);
            GameObjectDisplayInfoStore = new((dynamic)OpenDbc("GameObjectDisplayInfo"));
            if (gameFiles.WoWVersion == GameFilesVersion.Legion_7_3_5)
            {
                ItemAppearanceStore = new((dynamic)OpenDbc("ItemAppearance"));
                ItemModifiedAppearanceStore = new((dynamic)OpenDbc("ItemModifiedAppearance"));
            }
            else
                ItemAppearanceStore = new();
            HelmetGeosetVisDataStore = new((dynamic)OpenDbc("HelmetGeosetVisData"));
            ChrRacesStore = new((dynamic)OpenDbc("ChrRaces"));
            CharacterFacialHairStylesStore = new((dynamic)OpenDbc("CharacterFacialHairStyles"), gameFiles.WoWVersion);
            CharHairGeosetsStore = new((dynamic)OpenDbc("CharHairGeosets"), gameFiles.WoWVersion);
            MapStore = new ((dynamic)OpenDbc("Map"), gameFiles.WoWVersion);
            LiquidTypeStore = new((dynamic)OpenDbc("LiquidType"));
            LiquidMaterialStore = new((dynamic)OpenDbc("LiquidMaterial"));
            CurrencyTypeStore = new((dynamic)OpenDbc("CurrencyTypes"));
            if (gameFiles.WoWVersion == GameFilesVersion.Wrath_3_3_5a)
            {
                LightIntParamStore = new ((dynamic)OpenDbc("LightIntBand"));
                LightFloatParamStore = new ((dynamic)OpenDbc("LightFloatBand"));
                LightDataStore = new();
                TextureFileDataStore = new();
                ModelFileDataStore = new();
                LiquidObjectStore = new();
            }
            else if (gameFiles.WoWVersion == GameFilesVersion.Cataclysm_4_3_4)
            {
                LightIntParamStore = new ((dynamic)OpenDbc("LightIntBand"));
                LightFloatParamStore = new ((dynamic)OpenDbc("LightFloatBand"));
                LightDataStore = new();
                TextureFileDataStore = new();
                ModelFileDataStore = new();
                LiquidObjectStore = new((dynamic)OpenDbc("LiquidObject"));
            }
            else if (gameFiles.WoWVersion == GameFilesVersion.Mop_5_4_8)
            {
                LightIntParamStore = new ();
                LightFloatParamStore = new ();
                LightDataStore = new((dynamic)OpenDbc("LightData"));      
                TextureFileDataStore = new();
                ModelFileDataStore = new();
                LiquidObjectStore = new((dynamic)OpenDbc("LiquidObject"));
            }
            else
            {
                LightIntParamStore = new ();
                LightFloatParamStore = new ();
                LightDataStore = new((dynamic)OpenDbc("LightData"));
                TextureFileDataStore = new((dynamic)OpenDbc("TextureFileData"));
                ModelFileDataStore = new((dynamic)OpenDbc("ModelFileData"));
                LiquidObjectStore = new((dynamic)OpenDbc("LiquidObject"));
            }
            ItemStore = new((dynamic)OpenDbc("Item"), ItemModifiedAppearanceStore, ItemAppearanceStore);
            CharSectionsStore = new((dynamic)OpenDbc("CharSections"), TextureFileDataStore);
            ItemDisplayInfoStore = new((dynamic)OpenDbc("ItemDisplayInfo"), gameFiles.WoWVersion, ModelFileDataStore, TextureFileDataStore);
            LightParamStore = new (gameFiles.WoWVersion, (dynamic)OpenDbc("LightParams"), LightIntParamStore, LightFloatParamStore, LightDataStore);
            LightStore = new ((dynamic)OpenDbc("Light"), LightParamStore);
            WorldMapAreaStore = new((dynamic)OpenDbc("WorldMapArea"), gameFiles.WoWVersion);
        }

        public IEnumerable<(System.Type, object)> Stores()
        {
            var properties = GetType().GetProperties().Where(prop => prop.Name.EndsWith("Store"));
            foreach (var property in properties)
            {
                var store = property.GetValue(this);
                if (store != null)
                    yield return (property.PropertyType, store);
            }
        }
    }
}