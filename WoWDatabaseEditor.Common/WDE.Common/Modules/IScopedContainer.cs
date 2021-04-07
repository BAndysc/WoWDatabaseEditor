using Prism.Ioc;

namespace WDE.Module
{
    public interface IScopedContainer : IContainerExtension
    {
        IScopedContainer CreateScope();
    }
}