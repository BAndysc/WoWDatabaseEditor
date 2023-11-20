using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.PointOfInterests;

[AutoRegister]
[SingleInstance]
[RequiresCore("Azeroth")]
public class AzerothPointOfInterestQueryGenerator : BaseInsertQueryProvider<IPointOfInterest>, IDeleteQueryProvider<IPointOfInterest>
{
    protected override object Convert(IPointOfInterest obj)
    {
        return new
        {
            ID = obj.Id,
            PositionX = obj.PositionX,
            PositionY = obj.PositionY,
            Icon = obj.Icon,
            Flags = obj.Flags,
            Importance = obj.Importance,
            Name = obj.Name
        };
    }

    public IQuery Delete(IPointOfInterest t)
    {
        return Queries.Table(TableName)
            .Where(x => x.Column<uint>("ID") == t.Id)
            .Delete();
    }
    
    public override DatabaseTable TableName => DatabaseTable.WorldTable("points_of_interest");
}