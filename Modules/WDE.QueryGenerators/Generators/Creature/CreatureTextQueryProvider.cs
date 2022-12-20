using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Creature;

[AutoRegister]
public class CreatureTextGenerator : IInsertQueryProvider<ICreatureText>, IDeleteQueryProvider<ICreatureText>
{
    public string TableName => "creature_text";
    
    public IQuery Insert(ICreatureText t)
    {
        return Queries.Table(TableName)
            .Insert(new
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
                comment = t.Comment
            });
    }

    public IQuery Delete(ICreatureText t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("CreatureID") == t.CreatureId)
            .Delete();
    }
}