using System.Collections.Generic;
using System.Linq;
using WDE.Common.CoreVersion;
using WDE.Common.Factories.Http;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.CoreVersion
{
    [SingleInstance]
    [AutoRegister]
    public class CurrentCoreVersionService : ICurrentCoreVersion, ICoreVersionsProvider, ICoreVersionSettings, IUserAgentPart
    {
        private readonly ICurrentCoreSettings settings;

        public CurrentCoreVersionService(IEnumerable<ICoreVersion> coreVersions, 
            ICurrentCoreSettings settings,
            IMessageBoxService messageBoxService)
        {
            this.settings = settings;
            AllVersions = coreVersions.ToList();
            Current = AllVersions.First(c => c is UnspecifiedCoreVersion);
            
            var savedVersion = settings.CurrentCore;
            
            if (savedVersion != null)
            {
                var found = AllVersions.FirstOrDefault(v => v.Tag == savedVersion);
                if (found != null)
                {
                    Current = found;
                }
                else
                {
                    messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetIcon(MessageBoxIcon.Error)
                        .SetTitle("Error while loading core version")
                        .SetMainInstruction("Unknown core version")
                        .SetContent($"You have set core version to {savedVersion} in settings, however such core is not found in modules. Switching back to unspecified. Fix that in the settings.")
                        .WithOkButton(true)
                        .Build());
                    UpdateCurrentVersion(Current);
                }
            }
            else
                UpdateCurrentVersion(Current);
        }
        
        public ICoreVersion Current { get; }
        public bool IsSpecified => Current is not UnspecifiedCoreVersion;

        public IEnumerable<ICoreVersion> AllVersions { get; }
        
        public void UpdateCurrentVersion(ICoreVersion version)
        {
            settings.UpdateCore(version);
        }

        public string Part => $"core: {Current.Tag}";
    }
}