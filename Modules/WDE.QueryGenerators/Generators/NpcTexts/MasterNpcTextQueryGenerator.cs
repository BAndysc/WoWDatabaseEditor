using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.NpcTexts;

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityMaster")]
public class MasterNpcTextQueryGenerator : BaseInsertQueryProvider<INpcTextFull>, IDeleteQueryProvider<INpcText>
{
    protected override object Convert(INpcTextFull text)
    {
        return new
        {
            ID = text.Id,
            BroadcastTextID0 = text.BroadcastTextID0,
            Probability0 = text.Probability0,
            BroadcastTextID1 = text.BroadcastTextID1,
            Probability1 = text.Probability1,
            BroadcastTextID2 = text.BroadcastTextID2,
            Probability2 = text.Probability2,
            BroadcastTextID3 = text.BroadcastTextID3,
            Probability3 = text.Probability3,
            BroadcastTextID4 = text.BroadcastTextID4,
            Probability4 = text.Probability4,
            BroadcastTextID5 = text.BroadcastTextID5,
            Probability5 = text.Probability5,
            BroadcastTextID6 = text.BroadcastTextID6,
            Probability6 = text.Probability6,
            BroadcastTextID7 = text.BroadcastTextID7,
            Probability7 = text.Probability7,
            VerifiedBuild = text.VerifiedBuild,
        };
    }

    public IQuery Delete(INpcText t)
    {
        return Queries.Table(TableName)
            .Where(x => x.Column<uint>("ID") == t.Id)
            .Delete();
    }
    
    public override DatabaseTable TableName => DatabaseTable.WorldTable("npc_text");
}