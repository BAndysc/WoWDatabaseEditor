using System.Collections.Generic;
using System.Linq;
using WDE.Common;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.QuickLoadService
{
    [UniqueProvider]
    public interface IQuickLoadSettings
    {
        void ApplySavedSettings(IList<QuickLoadItem> items);
        void Update(ICollection<QuickLoadItem> items);
        void Sort(IList<ISolutionItemProvider> items);
        bool IsVisible(ISolutionItemProvider item);
        bool IsNewLineSeparator(ISolutionItemProvider item);
        bool IsNewLineSeparator(string name);
    }

    [AutoRegister]
    [SingleInstance]
    public class QuickLoadSettings : IQuickLoadSettings
    {
        private readonly IUserSettings userSettings;
        private Data? savedData;

        public QuickLoadSettings(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            savedData = userSettings.Get<Data>(default);
        }

        public void Sort(IList<ISolutionItemProvider> items)
        {
            if (savedData?.Order == null) 
                return;
            
            items.Sort((a, b) =>
            {
                var indexA = savedData.Order.IndexIf(i => i == a!.GetName());
                var indexB = savedData.Order.IndexIf(i => i == b!.GetName());
                if (indexA == -1)
                    indexA = items.Count + items.IndexOf(a!);
                if (indexB == -1)
                    indexB = items.Count + items.IndexOf(b!);
                return indexA.CompareTo(indexB);
            });
        }

        public bool IsVisible(ISolutionItemProvider item)
        {
            if (item.ByDefaultHideFromQuickStart)
                return savedData?.Visible?.Contains(item.GetName()) ?? false;
            return !savedData?.Hidden?.Contains(item.GetName()) ?? true;
        }

        public bool IsVisible(QuickLoadItem item)
        {
            if (item.ByDefaultHidden)
                return savedData?.Visible?.Contains(item.Name) ?? false;
            return !savedData?.Hidden?.Contains(item.Name) ?? true;
        }

        public bool IsNewLineSeparator(ISolutionItemProvider item) => IsNewLineSeparator(item.GetName());

        public bool IsNewLineSeparator(string name)
        {
            if (savedData == null || savedData.Order == null)
                return false;
            var indexOf = savedData.Order.IndexOf(name);
            if (indexOf == -1)
                return false;
            if (indexOf == 0)
                return false;
            return savedData.Order[indexOf - 1] == "---";
        }

        public void ApplySavedSettings(IList<QuickLoadItem> items)
        {
            foreach (var i in items)
            {
                i.IsVisible = IsVisible(i);
            }
            
            if (savedData == null)
                return;

            if (savedData.Order != null)
            {
                items.Sort((a, b) =>
                {
                    var indexA = savedData.Order.IndexIf(i => i == a!.Name);
                    var indexB = savedData.Order.IndexIf(i => i == b!.Name);
                    if (indexA == -1)
                        indexA = items.Count + items.IndexOf(a!);
                    if (indexB == -1)
                        indexB = items.Count + items.IndexOf(b!);
                    return indexA.CompareTo(indexB);
                });
            }
        }

        public void Update(ICollection<QuickLoadItem> items)
        { 
            savedData = new Data
            {
                Order = items.Select(i => i.Name).ToArray(),
                Hidden = items.Where(i => !i.IsVisible && !i.ByDefaultHidden).Select(i => i.Name).ToArray(),
                Visible = items.Where(i => i.IsVisible && i.ByDefaultHidden).Select(i => i.Name).ToArray()
            };
            userSettings.Update(savedData);
        }
        
        public class Data : ISettings
        {
            public string[]? Order { get; set; }
            public string[]? Hidden { get; set; }
            public string[]? Visible { get; set; }
        }
    }
}