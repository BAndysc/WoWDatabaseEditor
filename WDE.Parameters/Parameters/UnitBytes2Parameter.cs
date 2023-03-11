using System;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Parameters.ViewModels;

namespace WDE.Parameters.Parameters
{
    public class UnitBytes2Parameter : Parameter, ICustomPickerParameter<long>
    {
        internal readonly IParameter<long> sheathStateParameter;
        internal readonly IParameter<long> pvpFlagsParameter;
        internal readonly IParameter<long> petFlagsParameter;
        internal readonly IParameter<long> shapeshiftFormParameter;
        private readonly Lazy<IWindowManager> windowManager;

        public UnitBytes2Parameter(IParameter<long> sheathStateParameter,
            IParameter<long> pvpFlagsParameter, 
            IParameter<long> petFlagsParameter,
            IParameter<long> shapeshiftFormParameter,
            Lazy<IWindowManager> windowManager)
        {
            this.sheathStateParameter = sheathStateParameter;
            this.pvpFlagsParameter = pvpFlagsParameter;
            this.petFlagsParameter = petFlagsParameter;
            this.shapeshiftFormParameter = shapeshiftFormParameter;
            this.windowManager = windowManager;
        }

        public override bool HasItems => true;

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

        public async Task<(long, bool)> PickValue(long value)
        {
            using var vm = new UnitBytes2EditorViewModel(this, value);
            if (await windowManager.Value.ShowDialog(vm))
                return (vm.Bytes2, true);
            return (0, false);
        }
    }
}