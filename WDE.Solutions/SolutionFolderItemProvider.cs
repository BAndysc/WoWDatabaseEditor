using System;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Solutions
{
    [AutoRegister]
    public class SolutionFolderItemProvider : ISolutionItemProvider
    {
        public bool IsCompatibleWithCore(ICoreVersion core) => true;

        public string GetName() => "Folder";

        public ImageUri GetImage() => new ("Resources/folder.png");

        public string GetDescription() => "Container for solutions";

        public async Task<ISolutionItem> CreateSolutionItem()
        {
            string input = Interaction.InputBox("Put folder name", "New Folder", "My new folder");
            if (!string.IsNullOrEmpty(input))
                return new SolutionFolderItem(input);
            return null;
        }
    }
}