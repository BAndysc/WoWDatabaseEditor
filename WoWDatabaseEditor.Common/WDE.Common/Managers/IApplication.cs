using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Managers
{
    [UniqueProvider]
    public interface IApplication
    {
        Task<bool> CanClose();
        Task<bool> TryClose();
        void ForceClose();
    }
}