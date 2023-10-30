using System;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Providers;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.InputEntryProviderService
{
    [AutoRegister]
    public class InputBoxService : IInputBoxService
    {
        private readonly Lazy<IWindowManager> windowManager;
        
        public InputBoxService(Lazy<IWindowManager> windowManager)
        {
            this.windowManager = windowManager;
        }
        
        public async Task<uint?> GetUInt(string title, string description)
        {
            using var vm = new InputEntryProviderViewModel<uint>(title, description, 0);
            if (await windowManager.Value.ShowDialog(vm))
                return vm.Entry;
            return null;
        }

        public async Task<string?> GetString(string title, string description, string defaultValue = "", bool multiline = false, bool allowEmpty = false)
        {
            using var vm = new InputEntryProviderViewModel<string>(title, description, defaultValue, s => allowEmpty || !string.IsNullOrEmpty(s), multiline);
            if (await windowManager.Value.ShowDialog(vm))
                return vm.Entry;
            return null;
        }
    }
}