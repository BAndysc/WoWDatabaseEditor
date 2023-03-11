using System;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Parameters.ViewModels;

namespace WDE.Parameters.Parameters
{
    public class UnitBytes1Parameter : Parameter, ICustomPickerParameter<long>
    {
        private readonly IParameter<long> standStateParameter;
        private readonly IParameter<long> animTierParameter;
        private readonly Lazy<IWindowManager> windowManager;

        public UnitBytes1Parameter(IParameter<long> standStateParameter, IParameter<long> animTierParameter,
            Lazy<IWindowManager> windowManager)
        {
            this.standStateParameter = standStateParameter;
            this.animTierParameter = animTierParameter;
            this.windowManager = windowManager;
        }

        public override bool HasItems => true;

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
                sb.Append(", Anim tier: " + animTierParameter.ToString(animTier));
            return sb.ToString();
        }

        public async Task<(long, bool)> PickValue(long value)
        {
            using var vm = new UnitBytes1EditorViewModel(value);
            if (await windowManager.Value.ShowDialog(vm))
                return (vm.Bytes1, true);
            return (0, false);
        }
    }
}