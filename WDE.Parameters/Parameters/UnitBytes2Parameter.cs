using System.Text;
using WDE.Common.Parameters;

namespace WDE.Parameters.Parameters
{
    public class UnitBytes2Parameter : Parameter
    {
        private readonly IParameter<long> sheathStateParameter;
        private readonly IParameter<long> pvpFlagsParameter;
        private readonly IParameter<long> petFlagsParameter;
        private readonly IParameter<long> shapeshiftFormParameter;

        public UnitBytes2Parameter(IParameter<long> sheathStateParameter,
            IParameter<long> pvpFlagsParameter, 
            IParameter<long> petFlagsParameter,
            IParameter<long> shapeshiftFormParameter)
        {
            this.sheathStateParameter = sheathStateParameter;
            this.pvpFlagsParameter = pvpFlagsParameter;
            this.petFlagsParameter = petFlagsParameter;
            this.shapeshiftFormParameter = shapeshiftFormParameter;
        }

        public override string ToString(long key)
        {
            var sheathState = key & 0xFF;
            var pvpFlags = (key >> 8) & 0xFF;
            var petFlags = (key >> 16) & 0xFF;
            var shapeShiftForm = (key >> 24) & 0xFF;

            StringBuilder sb = new();
            sb.Append("Sheath state: " + sheathStateParameter.ToString(sheathState));
            if (pvpFlags != 0)
                sb.Append(", PvP Flags: " + pvpFlagsParameter.ToString(pvpFlags));
            if (petFlags != 0)
                sb.Append(", Pet Flags: " + petFlagsParameter.ToString(petFlags));
            if (shapeShiftForm != 0)
                sb.Append(", Shape Shift Form: " + shapeshiftFormParameter.ToString(shapeShiftForm));
            return sb.ToString();
        }
    }
}