using WDE.Common.DBC;

namespace WDE.MpqReader.DBC
{
    public class LightParam
    {
        public readonly uint Id;
        public readonly uint HighlightSky;
        public readonly uint LightSkyboxID;
        public readonly float Glow;
        public readonly float WaterShallowAlpha;
        public readonly float WaterDeepAlpha;
        public readonly float OceanShallowAlpha;
        public readonly float OceanDeepAlpha;
        public readonly uint Flags;
        public readonly LightIntParam[] IntParams;
        public readonly LightFloatParam[] FloatParams;

        public LightIntParam GetLightParameter(LightIntParamType type)
        {
            return IntParams[(int)type];
        }
        
        public LightFloatParam GetLightParameter(LightFloatParamType type)
        {
            return FloatParams[(int)type];
        }
        
        public LightParam(IDbcIterator dbcIterator, LightIntParamStore lightIntParamStore, LightFloatParamStore floatParamStore)
        {
            int i = 0;
            Id = dbcIterator.GetUInt(i++);
            HighlightSky = dbcIterator.GetUInt(i++);
            LightSkyboxID = dbcIterator.GetUInt(i++);
            Glow = dbcIterator.GetFloat(i++);
            WaterShallowAlpha = dbcIterator.GetFloat(i++);
            WaterDeepAlpha = dbcIterator.GetFloat(i++);
            OceanShallowAlpha = dbcIterator.GetFloat(i++);
            OceanDeepAlpha = dbcIterator.GetFloat(i++);
            Flags = dbcIterator.GetUInt(i++);
            IntParams = new LightIntParam[17];
            FloatParams = new LightFloatParam[6];
            for (uint j = 0; j < 17; ++j)
                IntParams[j] = lightIntParamStore[j + Id * 18 - 17];
            for (uint j = 0; j < 6; ++j)
                FloatParams[j] = floatParamStore[j + Id * 6 - 5];
        }
    }

    public class Light
    {
        private static readonly float YardToInch = 36;
        
        public readonly uint Id;
        public readonly uint Continent;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float FalloffStart;
        public readonly float FalloffEnd;
        public readonly uint[] LightParamIds;
        public readonly LightParam?[] LightParams;

        public LightParam NormalWeather => LightParams[0];
        
        public Light(IDbcIterator row, LightParamStore lightParamStore)
        {
            int i = 0;
            Id = row.GetUInt(i++);
            Continent = row.GetUInt(i++);
            Y = 17066.666f - row.GetFloat(i++) / YardToInch;
            Z = row.GetFloat(i++) / YardToInch;
            X = 17066.666f - row.GetFloat(i++) / YardToInch;
            FalloffStart = row.GetFloat(i++) / YardToInch;
            FalloffEnd = row.GetFloat(i++) / YardToInch;
            LightParamIds = new uint[8];
            LightParams = new LightParam?[8];
            for (int j = 0; j < 8; j++)
            {
                LightParamIds[j] = row.GetUInt(i++);
                if (LightParamIds[j] != 0)
                {
                    LightParams[j] = lightParamStore[LightParamIds[j]];
                }
            }
        }
    }
}