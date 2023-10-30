using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Providers
{
    [UniqueProvider]
    public interface IInputBoxService
    {
        Task<uint?> GetUInt(string title, string description);
        Task<string?> GetString(string title, string description, string defaultValue = "", bool multiline = false, bool allowEmpty = false);
    }
}