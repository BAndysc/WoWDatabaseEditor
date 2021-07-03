using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Database
{
    [UniqueProvider]
    public interface IAuthDatabaseProvider
    {
        bool IsConnected { get; }

        Task<IList<IAuthRbacPermission>> GetRbacPermissionsAsync();
        Task<IList<IAuthRbacLinkedPermission>> GetLinkedPermissionsAsync();
    }
}