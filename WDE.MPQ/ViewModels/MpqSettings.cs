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
        private MpqOpenType openType;

        public MpqSettings(IUserSettings userSettings, IWoWFilesVerifier verifier)
        {
            this.userSettings = userSettings;
            this.verifier = verifier;
            var saved = userSettings.Get<Data>();

            if (verifier.VerifyFolder(saved.Path) != WoWFilesType.Invalid)
                path = saved.Path;

            openType = saved.OpenType;
        }

        private struct Data : ISettings
        {
            public string? Path { get; set; }
            public MpqOpenType OpenType { get; set; }
        }
        
        public string? Path
        {
            get => path;
            set => path = value;
        }

        public MpqOpenType OpenType
        {
            get => openType;
            set => openType = value;
        }

        public void Save()
        {
            userSettings.Update(new Data(){Path = path, OpenType = openType});
        }
    }
}