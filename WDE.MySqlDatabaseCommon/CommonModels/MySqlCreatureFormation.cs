using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "creature_formations")]
public class MySqlCreatureFormation : ICreatureFormation
{
    [Column(Name = "leaderGUID")]
    public uint LeaderGuid { get; set; }

    [PrimaryKey]
    [Column(Name = "memberGUID")]
    public uint MemberGuid { get; set; }

    [Column(Name = "dist")]
    public virtual float Dist { get; set; }

    [Column(Name = "angle")]
    public virtual float Angle { get; set; }

    [Column(Name = "groupAI")]
    public uint GroupAi { get; set; }

    [Column(Name = "point_1")]
    public virtual uint Point1 { get; set; }

    [Column(Name = "point_2")]
    public virtual uint Point2 { get; set; }
}
