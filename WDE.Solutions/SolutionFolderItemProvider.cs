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

        public ImageUri GetImage() => new ("Icons/folder_big.png");

        public string GetDescription() => "Container for solutions";
        
        public string GetGroupName() => "Other";

        public bool IsContainer => true;

        public Task<ISolutionItem?> CreateSolutionItem()
        {
            throw new Exception("You are not supposed to call this!");
        }

        public Task<ISolutionItem?> CreateSolutionItem(string name)
        {
            if (!string.IsNullOrEmpty(name))
                return Task.FromResult<ISolutionItem?>(new SolutionFolderItem(name));
            return Task.FromResult<ISolutionItem?>(null);
        }
    }
}