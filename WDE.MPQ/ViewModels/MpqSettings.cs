using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.MPQ.ViewModels
{
    [AutoRegister]
    [SingleInstance]
    internal class MpqSettings : IMpqSettings
    {
        private readonly IUserSettings userSettings;
        private readonly IWoWFilesVerifier verifier;
        private string? path;

        public MpqSettings(IUserSettings userSettings, IWoWFilesVerifier verifier)
        {
            this.userSettings = userSettings;
            this.verifier = verifier;
            var saved = userSettings.Get<Data>();

            if (verifier.VerifyFolder(saved.Path))
                path = saved.Path;
        }

        private struct Data : ISettings
        {
            public string? Path { get; set; }
        }
        
        public string? Path
        {
            get => path;
            set
            {
                path = value;
                userSettings.Update(new Data(){Path = value});
            }
        }
    }
}