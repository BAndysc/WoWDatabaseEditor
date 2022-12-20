using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC
{
    public class LightParam
    {
        public readonly uint Id;
        public readonly uint HighlightSky;
        public readonly uint LightSkyboxID;
        public readonly uint? CloudTypeId;
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
        
        public LightParam(GameFilesVersion ver, IDbcIterator dbcIterator, LightIntParamStore lightIntParamStore,
            LightFloatParamStore floatParamStore, LightDataStore lightDataStore)
        {
            int i = 0;
            Id = dbcIterator.GetUInt(i++);
            HighlightSky = dbcIterator.GetUInt(i++);
            LightSkyboxID = dbcIterator.GetUInt(i++);
            if (ver == GameFilesVersion.Cataclysm_4_3_4)
                CloudTypeId = dbcIterator.GetUInt(i++);
            Glow = dbcIterator.GetFloat(i++);
            WaterShallowAlpha = dbcIterator.GetFloat(i++);
            WaterDeepAlpha = dbcIterator.GetFloat(i++);
            OceanShallowAlpha = dbcIterator.GetFloat(i++);
            OceanDeepAlpha = dbcIterator.GetFloat(i++);
            Flags = dbcIterator.GetUInt(i++);
            if (ver <= GameFilesVersion.Cataclysm_4_3_4)
            {
                IntParams = new LightIntParam[17];
                FloatParams = new LightFloatParam[6];
                for (uint j = 0; j < 17; ++j)
                    IntParams[j] = lightIntParamStore[j + Id * 18 - 17];
                for (uint j = 0; j < 6; ++j)
                    FloatParams[j] = floatParamStore[j + Id * 6 - 5];                
            }
            else
            {
                IntParams = new LightIntParam[18];
                FloatParams = new LightFloatParam[6];
                LoadFromDataStore(lightDataStore);
            }
        }
        
        public LightParam(GameFilesVersion ver, IWdcIterator dbcIterator, LightDataStore dataStore)
        {
            int i = 0;
            Id = (uint)dbcIterator.Id;
            HighlightSky = dbcIterator.GetByte("HighlightSky");
            LightSkyboxID = dbcIterator.GetByte("LightSkyboxID");
            CloudTypeId = dbcIterator.GetByte("CloudTypeID");
            Glow = dbcIterator.GetFloat("Glow");
            WaterShallowAlpha = dbcIterator.GetFloat("WaterShallowAlpha");
            WaterDeepAlpha = dbcIterator.GetFloat("WaterDeepAlpha");
            OceanShallowAlpha = dbcIterator.GetFloat("OceanShallowAlpha");
            OceanDeepAlpha = dbcIterator.GetFloat("OceanDeepAlpha");
            Flags = dbcIterator.GetByte("Flags");
            IntParams = new LightIntParam[18];
            FloatParams = new LightFloatParam[6];
            LoadFromDataStore(dataStore);
        }

        private void LoadFromDataStore(LightDataStore dataStore)
        {
            var data = dataStore.GetDataByLight((ushort)Id);
            if (data == null)
            {
                for (uint j = 0; j < IntParams.Length; ++j)
                    IntParams[j] = LightIntParam.Empty;
                for (uint j = 0; j < FloatParams.Length; ++j)
                    FloatParams[j] = LightFloatParam.Empty;
            }
            else
            {
                var times = data.Select(d => Time.FromHalfMinutes(d.Time)).ToArray();

                IntParams[(int)LightIntParamType.GeneralLightning] =
                    new LightIntParam(data.Select(d => d.DirectColor).ToArray(), times);
                IntParams[(int)LightIntParamType.AmbientLight] =
                    new LightIntParam(data.Select(d => d.AmbientColor).ToArray(), times);
                IntParams[(int)LightIntParamType.SkyTopMost] =
                    new LightIntParam(data.Select(d => d.SkyTopColor).ToArray(), times);
                IntParams[(int)LightIntParamType.SkyMiddle] =
                    new LightIntParam(data.Select(d => d.SkyMiddleColor).ToArray(), times);
                IntParams[(int)LightIntParamType.SkyToHorizon] =
                    new LightIntParam(data.Select(d => d.SkyBand1Color).ToArray(), times);
                IntParams[(int)LightIntParamType.SkyJustAboveHorizon] =
                    new LightIntParam(data.Select(d => d.SkyBand2Color).ToArray(), times);
                IntParams[(int)LightIntParamType.SkyHorizon] =
                    new LightIntParam(data.Select(d => d.SkySmogColor).ToArray(), times);
                IntParams[(int)LightIntParamType.FogInTheBackground] =
                    new LightIntParam(data.Select(d => d.SkyFogColor).ToArray(), times);
                IntParams[(int)LightIntParamType.SunColor] = new LightIntParam(data.Select(d => d.SunColor).ToArray(), times);
                IntParams[(int)LightIntParamType.Clouds1] =
                    new LightIntParam(data.Select(d => d.CloudLayer1AmbientColor).ToArray(), times);
                IntParams[(int)LightIntParamType.Clouds2] =
                    new LightIntParam(data.Select(d => d.CloudLayer2AmbientColor).ToArray(), times);
                IntParams[(int)LightIntParamType.Clouds3] =
                    new LightIntParam(data.Select(d => d.CloudEmissiveColor).ToArray(), times);
                IntParams[(int)LightIntParamType.OceanShallow] =
                    new LightIntParam(data.Select(d => d.OceanCloseColor).ToArray(), times);
                IntParams[(int)LightIntParamType.OceanDeep] =
                    new LightIntParam(data.Select(d => d.OceanFarColor).ToArray(), times);
                IntParams[(int)LightIntParamType.RiverShallow] =
                    new LightIntParam(data.Select(d => d.RiverCloseColor).ToArray(), times);
                IntParams[(int)LightIntParamType.RiverDeep] =
                    new LightIntParam(data.Select(d => d.RiverFarColor).ToArray(), times);

                FloatParams[(int)LightFloatParamType.CloudDensity] =
                    new LightFloatParam(data.Select(d => d.CloudDensity).ToArray(), times);
            }
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
        
        public Light(IWdcIterator row, LightParamStore lightParamStore)
        {
            int i = 0;
            Id = (uint)row.Id;
            Continent = (ushort)row.GetShort("ContinentID");
            X = row.GetFloat("GameCoords", 0); //17066.666f -  / YardToInch;
            Y = row.GetFloat("GameCoords", 1); //17066.666f -  / YardToInch;
            Z = row.GetFloat("GameCoords", 2);// / YardToInch;
            FalloffStart = row.GetFloat("GameFalloffStart") / YardToInch;
            FalloffEnd = row.GetFloat("GameFalloffEnd") / YardToInch;
            LightParamIds = new uint[8];
            LightParams = new LightParam?[8];
            for (int j = 0; j < 8; j++)
            {
                LightParamIds[j] = row.GetUShort("LightParamsID", j);
                if (LightParamIds[j] != 0)
                {
                    LightParams[j] = lightParamStore[LightParamIds[j]];
                }
            }
        }
    }
}