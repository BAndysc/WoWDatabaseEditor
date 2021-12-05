using WDE.Common.DBC;
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
        public ItemDisplayInfoStore ItemDisplayInfoStore { get; }
        public CreatureModelDataStore CreatureModelDataStore { get; }
        public GameObjectDisplayInfoStore GameObjectDisplayInfoStore { get; }
        public CharSectionsStore CharSectionsStore { get; }
        // public ChrRacesStore ChrRacesStore { get; }
        public CharacterFacialHairStylesStore CharacterFacialHairStylesStore { get; }
        public CharHairGeosetsStore CharHairGeosetsStore { get; }
        public MapStore MapStore { get; }
        public LightIntParamStore LightIntParamStore { get; }
        public LightFloatParamStore LightFloatParamStore { get; }
        public LightParamStore LightParamStore { get; }
        public LightStore LightStore { get; }

        private IEnumerable<IDbcIterator> OpenDbc(string name)
        {
            return opener.Open(gameFiles.ReadFileSync($"DBFilesClient\\{name}.dbc"));
        }

        public DbcManager(IGameFiles gameFiles, IDatabaseClientFileOpener opener)
        {
            this.gameFiles = gameFiles;
            this.opener = opener;
            AreaTableStore = new(OpenDbc("AreaTable"));
            AreaTriggerStore = new(OpenDbc("AreaTrigger"));
            CreatureDisplayInfoStore = new(OpenDbc("CreatureDisplayInfo"));
            CreatureDisplayInfoExtraStore = new(OpenDbc("CreatureDisplayInfoExtra")); // for humanoids
            CreatureModelDataStore = new(OpenDbc("CreatureModelData"));
            GameObjectDisplayInfoStore = new(OpenDbc("GameObjectDisplayInfo"));
            ItemDisplayInfoStore = new(OpenDbc("ItemDisplayInfo"));
            CharSectionsStore = new(OpenDbc("CharSections"));
            // ChrRacesStore = new(OpenDbc("ChrRaces"));
            CharacterFacialHairStylesStore = new(OpenDbc("CharacterFacialHairStyles"));
            CharHairGeosetsStore = new(OpenDbc("CharHairGeosets"));
            MapStore = new (OpenDbc("Map"));
            LightIntParamStore = new (OpenDbc("LightIntBand"));
            LightFloatParamStore = new (OpenDbc("LightFloatBand"));
            LightParamStore = new (OpenDbc("LightParams"), LightIntParamStore, LightFloatParamStore);
            LightStore = new (OpenDbc("Light"), LightParamStore);
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