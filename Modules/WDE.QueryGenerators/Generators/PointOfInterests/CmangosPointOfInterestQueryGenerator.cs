using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.PointOfInterests;

[AutoRegister]
[SingleInstance]
[RequiresCore("CMaNGOS-TBC", "CMaNGOS-Classic", "CMaNGOS-WoTLK")]
public class CmangosPointOfInterestQueryGenerator : BaseInsertQueryProvider<IPointOfInterest>, IDeleteQueryProvider<IPointOfInterest>
{
    protected override object Convert(IPointOfInterest obj)
    {
        return new
        {
            entry = obj.Id,
            x = obj.PositionX,
            y = obj.PositionY,
            icon = obj.Icon,
            flags = obj.Flags,
            data = obj.Importance,
            icon_name = obj.Name
        };
    }

    public IQuery Delete(IPointOfInterest t)
    {
        return Queries.Table(TableName)
            .Where(x => x.Column<uint>("entry") == t.Id)
            .Delete();
    }
    
    public override DatabaseTable TableName => DatabaseTable.WorldTable("points_of_interest");
}