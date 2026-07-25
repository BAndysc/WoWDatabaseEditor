using System.Threading.Tasks;
using Prism.Ioc;
using WDE.CMangosConditions.ViewModels;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.CMangosConditions.Services
{
    [UniqueProvider]
    public interface IStandaloneMangosConditionsService
    {
        Task OpenStandaloneConditionsEditor();
    }

    [AutoRegister]
    [SingleInstance]
    internal class StandaloneMangosConditionsService : IStandaloneMangosConditionsService
    {
        private readonly IWindowManager windowManager;
        private readonly IContainerProvider containerProvider;
        private IAbstractWindowView? currentWindow;

        public StandaloneMangosConditionsService(IWindowManager windowManager, IContainerProvider containerProvider)
        {
            this.windowManager = windowManager;
            this.containerProvider = containerProvider;
        }

        public async Task OpenStandaloneConditionsEditor()
        {
            if (currentWindow != null)
            {
                currentWindow.Activate();
                return;
            }

            using var vm = containerProvider.Resolve<StandaloneMangosConditionsViewModel>();
            currentWindow = windowManager.ShowWindow(vm, out var lifetime);
            await lifetime;
            currentWindow = null;
        }
    }
}
