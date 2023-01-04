using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Modules;

[NonUniqueProvider]
public interface IGlobalAsyncInitializer
{
    public Task Initialize();
}