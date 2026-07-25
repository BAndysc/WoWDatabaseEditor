using LinqToDB.Mapping;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models;

// Cataclysm creature_formations renamed several columns:
//   dist -> FollowDistance, angle -> FollowAngle, point_1 -> InversionPoint1, point_2 -> InversionPoint2.
[Table(Name = "creature_formations")]
public class MySqlCreatureFormationCata : MySqlCreatureFormation
{
    [Column(Name = "FollowDistance")]
    public override float Dist { get; set; }

    [Column(Name = "FollowAngle")]
    public override float Angle { get; set; }

    [Column(Name = "InversionPoint1")]
    public override uint Point1 { get; set; }

    [Column(Name = "InversionPoint2")]
    public override uint Point2 { get; set; }
}
