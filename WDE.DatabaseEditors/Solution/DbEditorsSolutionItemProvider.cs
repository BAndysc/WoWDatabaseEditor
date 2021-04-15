using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Types;

namespace WDE.DatabaseEditors.Solution
{
    public abstract class DbEditorsSolutionItemProvider : ISolutionItemProvider
    {
        private readonly string itemName;
        private readonly string itemDescription;
        private readonly ImageUri itemIcon;

        protected DbEditorsSolutionItemProvider(string itemName, string itemDescription, string itemIconName)
        {
            this.itemName = itemName;
            this.itemDescription = itemDescription;
            this.itemIcon = new ImageUri($"Resources/{itemIconName}.png");
        }

        public string GetName() => itemName;

        public ImageUri GetImage() => itemIcon;

        public string GetDescription() => itemDescription;
        public string GetGroupName() => "Other";

        public bool IsCompatibleWithCore(ICoreVersion core) => true;

        public abstract Task<ISolutionItem?> CreateSolutionItem();
    }
}