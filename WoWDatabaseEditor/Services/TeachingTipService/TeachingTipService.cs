using System.Collections.Generic;
using System.Linq;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.TeachingTipService
{
    [AutoRegister]
    public class TeachingTipService : ITeachingTipService
    {
        private readonly IUserSettings userSettings;
        private HashSet<string> taughtTips = new();
        
        public TeachingTipService(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            var savedTips = userSettings.Get<Data>();
            if (savedTips.TaughtTips != null)
                foreach (var tip in savedTips.TaughtTips)
                    taughtTips.Add(tip);
        }
        
        public bool IsTipShown(string id)
        {
            return taughtTips.Contains(id);
        }

        public bool ShowTip(string id)
        {
            if (taughtTips.Add(id))
            {
                Save();
                return true;
            }

            return false;
        }

        private void Save()
        {
            userSettings.Update(new Data(){TaughtTips = taughtTips.ToList()});
        }

        public struct Data : ISettings
        {
            public List<string>? TaughtTips { get; set; }
        }
    }
}