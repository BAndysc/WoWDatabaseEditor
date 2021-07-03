using LinqToDB;
using LinqToDB.Data;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.SkyFireMySqlDatabase.Models
{
    public class SkyFireAuthDatabase : DataConnection
    {
        public SkyFireAuthDatabase() : base("SkyFireAuth")
        {
        }

        public ITable<RbacPermission> RbacPermissions => GetTable<RbacPermission>();
        public ITable<RbacLinkedPermission> RbacLinkedPermissions => GetTable<RbacLinkedPermission>();
    }
}