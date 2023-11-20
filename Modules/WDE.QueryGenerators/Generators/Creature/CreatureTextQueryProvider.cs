using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Creature;

[AutoRegister]
[RequiresCore("TrinityMaster", "TrinityCata", "TrinityWrath", "Azeroth")]
public class CreatureTextGenerator : BaseInsertQueryProvider<ICreatureText>, IDeleteQueryProvider<ICreatureText>
{
    public override DatabaseTable TableName => DatabaseTable.WorldTable("creature_text");
    
    protected override object Convert(ICreatureText t)
    {
        return new
        {
            CreatureID = t.CreatureId,
            GroupID = t.GroupId,
            ID = t.Id,
            BroadcastTextId = t.BroadcastTextId,
            Text = t.Text,
            Type = (int)t.Type,
            Language = t.Language,
            Probability = t.Probability,
            Emote = t.Emote,
            Duration = t.Duration,
            Sound = t.Sound,
            comment = t.Comment,
            __comment = t.__comment
        };
    }

    public IQuery Delete(ICreatureText t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("CreatureID") == t.CreatureId)
            .Delete();
    }
}

[AutoRegister]
[RequiresCore("CMaNGOS-TBC", "CMaNGOS-Classic", "CMaNGOS-WoTLK")]
public class CmangosCreatureTextGenerator : NotSupportedQueryProvider<ICreatureText>
{
    public override DatabaseTable TableName => DatabaseTable.WorldTable("creature_text");
}
