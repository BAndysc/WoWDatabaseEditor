using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LightFloatParam : LightParam<float>
{
    public LightFloatParam(IDbcIterator dbcIterator) : base(dbcIterator, (dbc, i) => dbc.GetFloat(i))
    {
    }
    
    protected LightFloatParam() {} 

    public LightFloatParam(float[] values, Time[] times) : base(values, times) {}
    
    public static LightFloatParam Empty => new LightFloatParam();

    protected override float Lerp(float lower, float higher, float t)
    {
        return lower + (higher - lower) * t;
    }
}