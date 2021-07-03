using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.Database.Auth
{
    public class AuthDatabaseDecorator : IAuthDatabaseProvider
    {
        protected IAuthDatabaseProvider impl;

        public AuthDatabaseDecorator(IAuthDatabaseProvider impl)
        {
            this.impl = impl;
        }

        public bool IsConnected => impl.IsConnected;
        public Task<IList<IAuthRbacPermission>> GetRbacPermissionsAsync() => impl.GetRbacPermissionsAsync();
        public Task<IList<IAuthRbacLinkedPermission>> GetLinkedPermissionsAsync() => impl.GetLinkedPermissionsAsync();
    }
}