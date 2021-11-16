using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class DbcManager
    {
        private readonly IGameContext gameContext;
        
        public LightIntParamStore LightIntParamStore { get; }
        public LightFloatParamStore LightFloatParamStore { get; }
        public LightParamStore LightParamStore { get; }
        public LightStore LightStore { get; }

        public DbcManager(IGameContext gameContext, IDatabaseClientFileOpener opener)
        {
            this.gameContext = gameContext;
            LightIntParamStore = new LightIntParamStore(opener.Open(gameContext.ReadFileSync("DBFilesClient\\LightIntBand.dbc")));
            LightFloatParamStore = new LightFloatParamStore(opener.Open(gameContext.ReadFileSync("DBFilesClient\\LightFloatBand.dbc")));
            LightParamStore = new LightParamStore(opener.Open(gameContext.ReadFileSync("DBFilesClient\\LightParams.dbc")), LightIntParamStore, LightFloatParamStore);
            LightStore = new LightStore(opener.Open(gameContext.ReadFileSync("DBFilesClient\\Light.dbc")), LightParamStore);
        }
    }
}