using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators;

// creature_formations stores leader↔member follow links. The Delete provider deletes the WHOLE
// leader group (all members sharing leaderGUID) - the formation editor saves a group by deleting
// it and bulk-inserting the current members.
[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityWrath", "Azeroth", "TrinityMaster", "TrinityCata")]
public class CreatureFormationQueryProvider : BaseInsertQueryProvider<ICreatureFormation>, IDeleteQueryProvider<ICreatureFormation>
{
    protected override object Convert(ICreatureFormation f) => new
    {
        leaderGUID = f.LeaderGuid,
        memberGUID = f.MemberGuid,
        dist = f.Dist,
        angle = f.Angle,
        groupAI = f.GroupAi,
        point_1 = f.Point1,
        point_2 = f.Point2,
    };

    public IQuery Delete(ICreatureFormation f) =>
        Queries.Table(TableName).Where(row => row.Column<uint>("leaderGUID") == f.LeaderGuid).Delete();

    public override DatabaseTable TableName => DatabaseTable.WorldTable("creature_formations");
}
