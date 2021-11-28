using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class DbcManager
    {
        private readonly IGameContext gameContext;
        private readonly IDatabaseClientFileOpener opener;

        public AreaTriggerStore AreaTriggerStore { get; }
        public CreatureDisplayInfoStore CreatureDisplayInfoStore { get; }
        // public CreatureDisplayInfoExtraStore CreatureDisplayInfoExtraStore { get; }
        public CreatureModelDataStore CreatureModelDataStore { get; }
        public MapStore MapStore { get; }
        public LightIntParamStore LightIntParamStore { get; }
        public LightFloatParamStore LightFloatParamStore { get; }
        public LightParamStore LightParamStore { get; }
        public LightStore LightStore { get; }

        private IEnumerable<IDbcIterator> OpenDbc(string name)
        {
            return opener.Open(gameContext.ReadFileSync($"DBFilesClient\\{name}.dbc"));
        }

        public DbcManager(IGameContext gameContext, IDatabaseClientFileOpener opener)
        {
            this.gameContext = gameContext;
            this.opener = opener;
            AreaTriggerStore = new(OpenDbc("AreaTrigger"));
            CreatureDisplayInfoStore = new(OpenDbc("CreatureDisplayInfo"));
            // CreatureDisplayInfoExtraStore = new(OpenDbc("CreatureDisplayInfoExtra")); // for humanoids
            CreatureModelDataStore = new(OpenDbc("CreatureModelData"));
            MapStore = new (OpenDbc("Map"));
            LightIntParamStore = new (OpenDbc("LightIntBand"));
            LightFloatParamStore = new (OpenDbc("LightFloatBand"));
            LightParamStore = new (OpenDbc("LightParams"), LightIntParamStore, LightFloatParamStore);
            LightStore = new (OpenDbc("Light"), LightParamStore);
        }
    }
}