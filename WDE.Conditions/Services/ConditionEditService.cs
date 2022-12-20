using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Conditions.ViewModels;
using WDE.Module.Attributes;

namespace WDE.Conditions.Services
{
    [AutoRegister]
    internal class ConditionEditService : IConditionEditService
    {
        private readonly IWindowManager windowManager;
        private readonly IContainerProvider containerProvider;

        public ConditionEditService(IWindowManager windowManager, IContainerProvider containerProvider)
        {
            this.windowManager = windowManager;
            this.containerProvider = containerProvider;
        }
    
        public async Task<IEnumerable<ICondition>?> EditConditions(int conditionSourceType, IReadOnlyList<ICondition>? conditions)
        {
            using var vm = containerProvider.Resolve<ConditionsEditorViewModel>(
                (typeof(IEnumerable<ICondition>), conditions ?? Enumerable.Empty<ICondition>()),
                (typeof(int), conditionSourceType));
            
            if (await windowManager.ShowDialog(vm))
            {
                return vm.GenerateConditions();
            }

            return null;
        }
    }
}