using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Fallbacks;

[FallbackAutoRegister]
[SingleInstance]
public class FallbackIconFinderService : IIconFinderService
{
    public async Task<ImageUri?> PickIconAsync()
    {
        return null;
    }

    public bool Enabled => false;
}