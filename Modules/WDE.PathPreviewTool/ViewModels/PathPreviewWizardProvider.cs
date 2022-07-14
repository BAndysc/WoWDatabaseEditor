using System;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Documents;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.PathPreviewTool.ViewModels;

[SingleInstance]
[AutoRegister]
public class PathPreviewWizardProvider : IWizardProvider
{
    private readonly Func<PathPreviewViewModel> creator;
    public string Name => "Path preview tool";
    public ImageUri Image => new ImageUri("Icons/document_minimap_big.png");
    public bool IsCompatibleWithCore(ICoreVersion core) => true;

    public PathPreviewWizardProvider(Func<PathPreviewViewModel> creator)
    {
        this.creator = creator;
    }
    
    public Task<IWizard> Create()
    {
        return Task.FromResult<IWizard>(creator());
    }
}