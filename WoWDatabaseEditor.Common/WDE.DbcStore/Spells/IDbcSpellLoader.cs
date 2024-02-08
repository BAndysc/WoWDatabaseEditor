using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Spells;

[NonUniqueProvider]
public interface IDbcSpellLoader : IDbcSpellService
{
    DBCVersions Version { get; }
    void Load(string path, DBCLocales dbcLocale);
}