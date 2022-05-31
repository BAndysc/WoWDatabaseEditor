using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader.DBC;

public class LightIntParam : LightParam<CArgb>
{
    public LightIntParam(IDbcIterator dbcIterator) : base(dbcIterator, (dbc, i) => CArgb.FromRGB(dbc.GetUInt(i)))
    {
    }

    protected override CArgb Lerp(CArgb colorLower, CArgb colorHigher, float t)
    {
        var r = colorLower.r + (colorHigher.r - colorLower.r) * t;
        var g = colorLower.g + (colorHigher.g - colorLower.g) * t;
        var b = colorLower.b + (colorHigher.b - colorLower.b) * t;
        return new CArgb((byte)r, (byte)g, (byte)b, 1);
    }

    public CArgb GetColorAtTime(Time time) => GetAtTime(time);
}