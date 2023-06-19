using WDE.Common.Tasks;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Loaders;

[NonUniqueProvider]
internal interface IDbcLoader
{
    DBCVersions Version { get; }
    void LoadDbc(DbcData data, ITaskProgress progress);
}