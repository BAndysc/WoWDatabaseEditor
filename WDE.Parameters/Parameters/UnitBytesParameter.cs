using System.Text;
using WDE.Common.Parameters;

namespace WDE.Parameters.Parameters
{
    public class UnitBytesPostMopParameter : Parameter
    {
        private readonly IParameter<long> raceParameter;
        private readonly IParameter<long> classParameter;
        private readonly IParameter<long> genderParameter;
        private readonly IParameter<long> powerParameter;

        public UnitBytesPostMopParameter(IParameter<long> raceParameter,
            IParameter<long> classParameter, 
            IParameter<long> genderParameter, 
            IParameter<long> powerParameter)
        {
            this.raceParameter = raceParameter;
            this.classParameter = classParameter;
            this.genderParameter = genderParameter;
            this.powerParameter = powerParameter;
        }

        public override string ToString(long key)
        {
            var race = key & 0xFF;
            var @class = (key >> 8) & 0xFF;
            var gender = (key >> 24) & 0xFF;

            StringBuilder sb = new();
            if (@class != 0)
                sb.Append("Class: " + classParameter.ToString(@class));
            if (race != 0)
                sb.Append(", Race: " + raceParameter.ToString(race));
            if (gender != 0)
                sb.Append(", Gender: " + genderParameter.ToString(gender));
            return sb.ToString();
        }
    }
    
    public class UnitBytesPreMopParameter : Parameter
    {
        private readonly IParameter<long> raceParameter;
        private readonly IParameter<long> classParameter;
        private readonly IParameter<long> genderParameter;
        private readonly IParameter<long> powerParameter;

        public UnitBytesPreMopParameter(IParameter<long> raceParameter,
            IParameter<long> classParameter, 
            IParameter<long> genderParameter, 
            IParameter<long> powerParameter)
        {
            this.raceParameter = raceParameter;
            this.classParameter = classParameter;
            this.genderParameter = genderParameter;
            this.powerParameter = powerParameter;
        }

        public override string ToString(long key)
        {
            var race = key & 0xFF;
            var @class = (key >> 8) & 0xFF;
            var gender = (key >> 16) & 0xFF;
            var powerType = (key >> 24) & 0xFF;

            StringBuilder sb = new();
            if (@class != 0)
                sb.Append("Class: " + classParameter.ToString(@class));
            if (race != 0)
                sb.Append(", Race: " + raceParameter.ToString(race));
            if (gender != 0)
                sb.Append(", Gender: " + genderParameter.ToString(gender));
            if (powerType != 0)
                sb.Append(", Power: " + powerParameter.ToString(powerType));
            return sb.ToString();
        }
    }
}