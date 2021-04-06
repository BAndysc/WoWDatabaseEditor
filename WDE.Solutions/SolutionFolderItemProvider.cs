using System;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Solutions
{
    [AutoRegister]
    public class SolutionFolderItemProvider : INamedSolutionItemProvider
    {
        public bool IsCompatibleWithCore(ICoreVersion core) => true;

        public string GetName() => "Folder";

        public ImageUri GetImage() => new ("Resources/folder.png");

        public string GetDescription() => "Container for solutions";
        
        public string GetGroupName() => "Other";

        public Task<ISolutionItem> CreateSolutionItem()
        {
            throw new Exception("You are not supposed to call this!");
        }

        public async Task<ISolutionItem> CreateSolutionItem(string name)
        {
            if (!string.IsNullOrEmpty(name))
                return new SolutionFolderItem(name);
            return null;
        }
    }
}