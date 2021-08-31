using System.Text;
using WDE.Common.Parameters;

namespace WDE.Parameters.Parameters
{
    public class UnitBytes1Parameter : Parameter
    {
        private readonly IParameter<long> standStateParameter;

        public UnitBytes1Parameter(IParameter<long> standStateParameter)
        {
            this.standStateParameter = standStateParameter;
        }

        public override string ToString(long key)
        {
            var standState = key & 0xFF;
            var petTalents = (key >> 8) & 0xFF;
            var visibilityState = (key >> 16) & 0xFF;
            var animTier = (key >> 24) & 0xFF;

            StringBuilder sb = new();
            sb.Append("Stand state: " + standStateParameter.ToString(standState));
            if (petTalents != 0)
                sb.Append(", Pet talents: " + petTalents);
            if (visibilityState != 0)
                sb.Append(", Visibility state: " + visibilityState);
            if (animTier != 0)
                sb.Append(", Anim tier: " + animTier);
            return sb.ToString();
        }
    }
}