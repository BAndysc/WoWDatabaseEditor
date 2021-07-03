using LinqToDB;
using LinqToDB.Data;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models
{
    public class TrinityAuthDatabase : DataConnection
    {
        public TrinityAuthDatabase() : base("TrinityAuth")
        {
        }

        public ITable<RbacPermission> RbacPermissions => GetTable<RbacPermission>();
        public ITable<RbacLinkedPermission> RbacLinkedPermissions => GetTable<RbacLinkedPermission>();
    }
}