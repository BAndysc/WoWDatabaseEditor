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

        public ITable<RbacPermission> RbacPermissions => this.GetTable<RbacPermission>();
        public ITable<RbacLinkedPermission> RbacLinkedPermissions => this.GetTable<RbacLinkedPermission>();
    }
}