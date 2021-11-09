using WDE.Common.CoreVersion;
using WDE.Common.Documents;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.MapRenderer;

[AutoRegister]
[SingleInstance]
public class GameProvider : IWizardProvider
{
    private readonly Func<GameViewModel> creator;
    public string Name => "WoW World";
    public ImageUri Image => new ImageUri("Icons/document_minimap_big.png");
    public bool IsCompatibleWithCore(ICoreVersion core) => true;
    
    public GameProvider(Func<GameViewModel> creator)
    {
        this.creator = creator;
    }
        
    public Task<IWizard> Create()
    {
        return Task.FromResult<IWizard>(creator());
    }
}