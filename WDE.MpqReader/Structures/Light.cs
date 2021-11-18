using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.Structures
{
    public struct Time : IComparable<Time>
    {
        private int minutes;
        
        public Time(int hours, int minutes)
        {
            this.minutes = hours * 60 + minutes;
        }
        
        public int Hour => minutes / 60;
        public int Minute => minutes % 60;
        public int TotalMinutes => minutes;
        public int HalfHourInMinutes => (int)Math.Round((minutes / 30.0f)) * 30;

        public bool Equals(Time other)
        {
            return minutes == other.minutes;
        }

        public override bool Equals(object? obj)
        {
            return obj is Time other && Equals(other);
        }

        public override int GetHashCode()
        {
            return minutes;
        }

        public int CompareTo(Time other)
        {
            return minutes.CompareTo(other.minutes);
        }

        public static bool operator <(Time left, Time right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Time left, Time right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(Time left, Time right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Time left, Time right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator ==(Time left, Time right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Time left, Time right)
        {
            return !left.Equals(right);
        }

        public static Time FromHalfMinutes(int minutes)
        {
            return new Time(0, minutes / 2);
        }
        
        public static Time FromMinutes(int minutes)
        {
            return new Time(0, minutes);
        }

        public static Time operator -(Time left, Time right)
        {
            var diff = left.minutes - right.minutes;
            if (diff < 0)
                diff += 1440;
            return new Time(0, diff);
        }
    }

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
    
    public class LightFloatParam : LightParam<float>
    {
        public LightFloatParam(IDbcIterator dbcIterator) : base(dbcIterator, (dbc, i) => dbc.GetFloat(i))
        {
        }

        protected override float Lerp(float lower, float higher, float t)
        {
            return lower + (higher - lower) * t;
        }
    }
    
    public abstract class LightParam<T>
    {
        public readonly uint Id;
        public readonly uint Count;
        public readonly Time[] Times;
        public readonly T[] Values;

        public T GetAtTime(Time time)
        {
            if (Count == 1)
                return Values[0];

            int higherThan = -1;
            int lowerThan = -1;

            for (int i = 0; i < Count; ++i)
            {
                if (Times[i] == time)
                    return Values[i];
                if (higherThan == -1 && Times[i] > time)
                    higherThan = i;
                if (lowerThan == -1 && Times[Count - i - 1] < time)
                    lowerThan = (int)Count - i - 1;
            }
            if (lowerThan == -1)
                lowerThan = (int)Count - 1;
            if (higherThan == -1)
                higherThan = 0;
            
            var diff = Times[higherThan] - Times[lowerThan];
            var timeToLower = time - Times[lowerThan];
            var timeToHigher = Times[higherThan] - time;

            var colorHigher = Values[higherThan];
            var colorLower = Values[lowerThan];

            return Lerp(colorLower, colorHigher, (float)timeToLower.TotalMinutes / diff.TotalMinutes);
        }

        protected abstract T Lerp(T colorLower, T colorHigher, float t);

        public LightParam(IDbcIterator dbcIterator, Func<IDbcIterator, int, T> reader)
        {
            int i = 0;
            Id = dbcIterator.GetUInt(i++);
            Count = dbcIterator.GetUInt(i++);
            Times = new Time[Count];
            Values = new T[Count];
            for (int j = 0; j < Count; j++)
                Times[j] = Time.FromHalfMinutes((int)dbcIterator.GetUInt(i++));
            i += (int)(16 - Count);
            for (int j = 0; j < Count; j++)
                Values[j] = reader(dbcIterator, i++);
        }
    }

    public class LightFloatParamStore
    {
        private Dictionary<uint, LightFloatParam> store = new();
        public LightFloatParamStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new LightFloatParam(row);
                store[o.Id] = o;
            }
        }
        
        public LightFloatParam this[uint id] => store[id];
    }

    public class LightIntParamStore
    {
        private Dictionary<uint, LightIntParam> store = new();
        public LightIntParamStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new LightIntParam(row);
                store[o.Id] = o;
            }
        }
        
        public LightIntParam this[uint id] => store[id];
    }

    public enum LightIntParamType
    {
        GeneralLightning = 0,
        AmbientLight = 1,
        SkyTopMost = 2,
        SkyMiddle = 3,
        SkyToHorizon = 4,
        SkyJustAboveHorizon = 5,
        SkyHorizon = 6,
        FogInTheBackground = 7,
        Unknown1 = 8,
        SunColor = 9,
        Clouds1 = 10,
        Clouds2 = 11,
        Clouds3 = 12,
        Unknown2 = 13,
        OceanShallow = 14,
        OceanDeep = 15,
        RiverShallow = 16,
        RiverDeep = 17
    }
    
    public enum LightFloatParamType
    {
        FogDistance = 0,
        FogMultiplier = 1,
        SunGodRays = 2,
        CloudDensity = 3,
        Unk1 = 4,
        Unk2 = 5
    }

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

    public class LightParamStore
    {
        private Dictionary<uint, LightParam> store = new();
        public LightParamStore(IEnumerable<IDbcIterator> rows, LightIntParamStore lightIntParamStore, LightFloatParamStore floatParamStore)
        {
            foreach (var row in rows)
            {
                var o = new LightParam(row, lightIntParamStore, floatParamStore);
                store[o.Id] = o;
            }
        }
        
        public LightParam this[uint id] => store[id];
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

    public class LightStore : IEnumerable<Light>
    {
        private Dictionary<uint, Light> store = new();
        public LightStore(IEnumerable<IDbcIterator> rows, LightParamStore lightParamStore)
        {
            foreach (var row in rows)
            {
                var o = new Light(row, lightParamStore);
                store[o.Id] = o;
            }
        }
        
        public Light this[uint id] => store[id];
        public IEnumerator<Light> GetEnumerator() => store.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
    }
}