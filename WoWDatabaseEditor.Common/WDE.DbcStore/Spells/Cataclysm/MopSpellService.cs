using WDE.DbcStore.FastReader;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Spells.Cataclysm;

public class MopSpellService : CataSpellService
{
    public MopSpellService(DatabaseClientFileOpener opener) : base(opener)
    {
    }
        
    public override DBCVersions Version => DBCVersions.MOP_18414;
}