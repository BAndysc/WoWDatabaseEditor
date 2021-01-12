using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Solutions.Providers
{
    [AutoRegister]
    public class FolderNameProvider : ISolutionNameProvider<SolutionFolderItem>
    {
        private readonly IDatabaseProvider database;
        private readonly ISpellStore spellStore;

        public FolderNameProvider(IDatabaseProvider database, ISpellStore spellStore)
        {
            this.database = database;
            this.spellStore = spellStore;
        }

        public string GetName(SolutionFolderItem item)
        {
            return item.MyName;
        }
    }
}