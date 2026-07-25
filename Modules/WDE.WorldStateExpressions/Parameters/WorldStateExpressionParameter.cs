using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.WorldStateExpressions.Models;
using WDE.WorldStateExpressions.ViewModels;

namespace WDE.WorldStateExpressions.Parameters
{
    internal class WorldStateExpressionParameter : IParameter<string>, ICustomPickerParameter<string>
    {
        private readonly IWindowManager windowManager;
        private readonly IContainerProvider containerProvider;

        public WorldStateExpressionParameter(IWindowManager windowManager, IContainerProvider containerProvider)
        {
            this.windowManager = windowManager;
            this.containerProvider = containerProvider;
        }

        public string? Prefix => null;
        public bool HasItems => true;
        public bool NeverUseComboBoxPicker => true;
        public Dictionary<string, SelectOption>? Items => null;

        /// set by the module once the WorldStateNameParameter is registered
        internal WorldStateNameParameter? WorldStateNames { get; set; }

        public string ToString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "(empty)";
            if (WorldStateExpressionCodec.TryDecode(value, out var expression, out _))
                return WorldStateExpressionCodec.ToReadable(expression, WorldStateNames == null ? null : WorldStateNames.NameOf);
            return value;
        }

        public async Task<(string, bool)> PickValue(string value)
        {
            WseExpression expression;
            string warning = "";
            if (string.IsNullOrWhiteSpace(value))
                expression = new WseExpression();
            else if (!WorldStateExpressionCodec.TryDecode(value, out expression, out var error))
                warning = $"Could not decode the current value ({error}) — saving will overwrite it.";

            using var vm = containerProvider.Resolve<WorldStateExpressionEditorViewModel>((typeof(WseExpression), expression));
            vm.ParseWarning = warning;
            if (await windowManager.ShowDialog(vm))
                return (vm.ToHex(), true);
            return ("", false);
        }
    }
}
