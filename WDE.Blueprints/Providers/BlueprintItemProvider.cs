using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Types;

namespace WDE.Blueprints.Providers
{
    //[AutoRegister]
    public class BlueprintItemProvider : ISolutionItemProvider
    {
        public bool IsCompatibleWithCore(ICoreVersion core) => false;
        
        public async Task<ISolutionItem> CreateSolutionItem()
        {
            return new BlueprintSolutionItem();
        }

        public string GetGroupName() => "Others";

        public string GetDescription()
        {
            return "Script in new Blueprints system";
        }

        public ImageUri GetImage() => new("Resources/blueprint_icon.png");

        public string GetName()
        {
            return "Blueprint";
        }
    }
}