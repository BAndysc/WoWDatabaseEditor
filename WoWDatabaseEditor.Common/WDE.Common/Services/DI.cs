using Unity;

namespace WDE.Common.Services;

public static class DI
{
    public static IUnityContainer Container { get; set; } = null!;
    
    public static T Resolve<T>() where T : class
    {
        return Container.Resolve<T>();
    }
}