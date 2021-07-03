using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.Database.Auth
{
    public class NullAuthDatabaseProvider : IAuthDatabaseProvider
    {
        public bool IsConnected => false;

        public Task<IList<IAuthRbacPermission>> GetRbacPermissionsAsync() =>
            Task.FromResult<IList<IAuthRbacPermission>>(new List<IAuthRbacPermission>());

        public Task<IList<IAuthRbacLinkedPermission>> GetLinkedPermissionsAsync() =>
            Task.FromResult<IList<IAuthRbacLinkedPermission>>(new List<IAuthRbacLinkedPermission>());
    }
}