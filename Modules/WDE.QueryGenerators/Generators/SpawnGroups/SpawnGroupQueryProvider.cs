using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;

namespace WDE.QueryGenerators.Generators.SpawnGroups;

[AutoRegister]
[SingleInstance]
[RequiresCore("Azeroth")]
public class SpawnGroupTemplateQueryProvider : NotSupportedQueryProvider<ISpawnGroupTemplate>
{
    public override DatabaseTable TableName => DatabaseTable.WorldTable("spawn_group_template");
}

[AutoRegister]
[SingleInstance]
[RequiresCore("Azeroth")]
public class SpawnGroupQueryProvider : NotSupportedQueryProvider<ISpawnGroupSpawn>
{
    public override DatabaseTable TableName => DatabaseTable.WorldTable("spawn_group");
}