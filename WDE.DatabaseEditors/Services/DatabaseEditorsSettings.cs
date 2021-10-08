using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services
{
    [SingleInstance]
    [AutoRegister]
    public class DatabaseEditorsSettings : IDatabaseEditorsSettings
    {
        private readonly IUserSettings userSettings;
        private Data data;
        
        public DatabaseEditorsSettings(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            data = userSettings.Get<Data>();
        }

        private struct Data : ISettings
        {
            public MultiRowSplitMode MultiRowSplitMode;
        }

        public MultiRowSplitMode MultiRowSplitMode
        {
            get => data.MultiRowSplitMode;
            set
            {
                data.MultiRowSplitMode = value;
                userSettings.Update(data);
            }
        }
    }
}