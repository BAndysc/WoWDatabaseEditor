using System.Threading.Tasks;
using Prism.Ioc;
using WDE.CMangosConditions.ViewModels;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.CMangosConditions.Services
{
    [UniqueProvider]
    public interface IStandaloneUnitConditionsService
    {
        Task OpenStandaloneUnitConditionsEditor();
    }

    [AutoRegister]
    [SingleInstance]
    internal class StandaloneUnitConditionsService : IStandaloneUnitConditionsService
    {
        private readonly IWindowManager windowManager;
        private readonly IContainerProvider containerProvider;
        private IAbstractWindowView? currentWindow;

        public StandaloneUnitConditionsService(IWindowManager windowManager, IContainerProvider containerProvider)
        {
            this.windowManager = windowManager;
            this.containerProvider = containerProvider;
        }

        public async Task OpenStandaloneUnitConditionsEditor()
        {
            if (currentWindow != null)
            {
                currentWindow.Activate();
                return;
            }

            using var vm = containerProvider.Resolve<StandaloneUnitConditionsViewModel>();
            currentWindow = windowManager.ShowWindow(vm, out var lifetime);
            await lifetime;
            currentWindow = null;
        }
    }
}
