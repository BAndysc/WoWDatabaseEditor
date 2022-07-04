using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader.DBC;

public class LightData
{
    public readonly uint Id;
    public readonly ushort LightParamID;
    public readonly CArgb DirectColor;
    public readonly CArgb AmbientColor;
    public readonly CArgb SkyTopColor;
    public readonly CArgb SkyMiddleColor;
    public readonly CArgb SkyBand1Color;
    public readonly CArgb SkyBand2Color;
    public readonly CArgb SkySmogColor;
    public readonly CArgb SkyFogColor;
    public readonly CArgb SunColor;
    public readonly CArgb CloudSunColor;
    public readonly CArgb CloudEmissiveColor;
    public readonly CArgb CloudLayer1AmbientColor;
    public readonly CArgb CloudLayer2AmbientColor;
    public readonly CArgb OceanCloseColor;
    public readonly CArgb OceanFarColor;
    public readonly CArgb RiverCloseColor;
    public readonly CArgb RiverFarColor;
    public readonly int ShadowOpacity;
    public readonly float FogEnd;
    public readonly float FogScaler;
    public readonly float CloudDensity;
    public readonly float FogDensity;
    public readonly float FogHeight;
    public readonly float FogHeightScaler;
    public readonly float FogHeightDensity;
    public readonly float SunFogAngle;
    public readonly float EndFogColorDistance;
    public readonly CArgb SunFogColor;
    public readonly CArgb EndFogColor;
    public readonly CArgb FogHeightColor;
    public readonly FileId ColorGradingFileDataID;
    public readonly CArgb HorizonAmbientColor;
    public readonly CArgb GroundAmbientColor;
    public readonly ushort Time;

    public LightData(IDbcIterator dbcIterator)
    {
        int i = 0;
        Id = dbcIterator.GetUInt(i++);
        LightParamID = (ushort)dbcIterator.GetInt(i++);
        Time = (ushort)dbcIterator.GetUInt(i++);
        DirectColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        AmbientColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        SkyTopColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        SkyMiddleColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        SkyBand1Color = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        SkyBand2Color = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        SkySmogColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        SkyFogColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        SunColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        CloudSunColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        CloudEmissiveColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        CloudLayer1AmbientColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        CloudLayer2AmbientColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        OceanCloseColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        OceanFarColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        RiverCloseColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        RiverFarColor = CArgb.FromRGB(dbcIterator.GetUInt(i++));
        ShadowOpacity = dbcIterator.GetInt(i++);
        FogEnd = dbcIterator.GetInt(i++);
        FogScaler = dbcIterator.GetFloat(i++);
        CloudDensity = dbcIterator.GetFloat(i++);
    }
    
    public LightData(IWdcIterator dbcIterator)
    {
        Id = (uint)dbcIterator.Id;
        LightParamID = dbcIterator.GetUShort("LightParamID");
        DirectColor = CArgb.FromRGB((uint)dbcIterator.GetInt("DirectColor"));
        AmbientColor = CArgb.FromRGB((uint)dbcIterator.GetInt("AmbientColor"));
        SkyTopColor = CArgb.FromRGB((uint)dbcIterator.GetInt("SkyTopColor"));
        SkyMiddleColor = CArgb.FromRGB((uint)dbcIterator.GetInt("SkyMiddleColor"));
        SkyBand1Color = CArgb.FromRGB((uint)dbcIterator.GetInt("SkyBand1Color"));
        SkyBand2Color = CArgb.FromRGB((uint)dbcIterator.GetInt("SkyBand2Color"));
        SkySmogColor = CArgb.FromRGB((uint)dbcIterator.GetInt("SkySmogColor"));
        SkyFogColor = CArgb.FromRGB((uint)dbcIterator.GetInt("SkyFogColor"));
        SunColor = CArgb.FromRGB((uint)dbcIterator.GetInt("SunColor"));
        CloudSunColor = CArgb.FromRGB((uint)dbcIterator.GetInt("CloudSunColor"));
        CloudEmissiveColor = CArgb.FromRGB((uint)dbcIterator.GetInt("CloudEmissiveColor"));
        CloudLayer1AmbientColor = CArgb.FromRGB((uint)dbcIterator.GetInt("CloudLayer1AmbientColor"));
        CloudLayer2AmbientColor = CArgb.FromRGB((uint)dbcIterator.GetInt("CloudLayer2AmbientColor"));
        OceanCloseColor = CArgb.FromRGB((uint)dbcIterator.GetInt("OceanCloseColor"));
        OceanFarColor = CArgb.FromRGB((uint)dbcIterator.GetInt("OceanFarColor"));
        RiverCloseColor = CArgb.FromRGB((uint)dbcIterator.GetInt("RiverCloseColor"));
        RiverFarColor = CArgb.FromRGB((uint)dbcIterator.GetInt("RiverFarColor"));
        ShadowOpacity = dbcIterator.GetInt("ShadowOpacity");
        FogEnd = dbcIterator.GetFloat("FogEnd");
        FogScaler = dbcIterator.GetFloat("FogScaler");
        CloudDensity = dbcIterator.GetFloat("CloudDensity");
        FogDensity = dbcIterator.GetFloat("FogDensity");
        FogHeight = dbcIterator.GetFloat("FogHeight");
        FogHeightScaler = dbcIterator.GetFloat("FogHeightScaler");
        FogHeightDensity = dbcIterator.GetFloat("FogHeightDensity");
        SunFogAngle = dbcIterator.GetFloat("SunFogAngle");
        EndFogColorDistance = dbcIterator.GetFloat("EndFogColorDistance");
        SunFogColor = CArgb.FromRGB(dbcIterator.GetUInt("SunFogColor"));
        EndFogColor = CArgb.FromRGB(dbcIterator.GetUInt("EndFogColor"));
        FogHeightColor = CArgb.FromRGB(dbcIterator.GetUInt("FogHeightColor"));
        ColorGradingFileDataID = dbcIterator.GetUInt("ColorGradingFileDataID");
        HorizonAmbientColor = CArgb.FromRGB((uint)dbcIterator.GetInt("HorizonAmbientColor"));
        GroundAmbientColor = CArgb.FromRGB((uint)dbcIterator.GetInt("GroundAmbientColor"));
        Time = dbcIterator.GetUShort("Time");
    }
}