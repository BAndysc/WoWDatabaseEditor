using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class DbcManager
    {
        private readonly IGameContext gameContext;
        private readonly IDatabaseClientFileOpener opener;

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
            MapStore = new (OpenDbc("Map"));
            LightIntParamStore = new (OpenDbc("LightIntBand"));
            LightFloatParamStore = new (OpenDbc("LightFloatBand"));
            LightParamStore = new (OpenDbc("LightParams"), LightIntParamStore, LightFloatParamStore);
            LightStore = new (OpenDbc("Light"), LightParamStore);
        }
    }
}