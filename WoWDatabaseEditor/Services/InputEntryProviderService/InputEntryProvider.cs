using System;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Providers;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.InputEntryProviderService
{
    [AutoRegister]
    public class InputEntryProvider : IInputEntryProvider
    {
        private readonly Lazy<IWindowManager> windowManager;
        
        public InputEntryProvider(Lazy<IWindowManager> windowManager)
        {
            this.windowManager = windowManager;
        }
        
        public async Task<uint?> GetEntry()
        {
            var vm = new InputEntryProviderViewModel();
            if (await windowManager.Value.ShowDialog(vm))
                return vm.Entry;
            return null;
        }
    }
}