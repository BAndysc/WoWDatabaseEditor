using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Database
{
    [UniqueProvider]
    public interface IAuthMySqlExecutor
    {
        bool IsConnected { get; }
        
        Task ExecuteSql(string query);
    }
}