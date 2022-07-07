using Prism.Ioc;
using Unity;

namespace WDE.Module
{
    public interface IScopedContainer : IContainerExtension<IUnityContainer>
    {
        IScopedContainer CreateScope();
        void Dispose();
    }
}