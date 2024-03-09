using LinqToDB;
using LinqToDB.Data;
//using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.CMMySqlDatabase.Models
{
    public class AuthDatabaseWoTLK : DataConnection
    {
        public AuthDatabaseWoTLK() : base("CMaNGOS-WoTLK-Auth")
        {
        }
// 
//         public ITable<RbacPermission> RbacPermissions => this.GetTable<RbacPermission>();
//         public ITable<RbacLinkedPermission> RbacLinkedPermissions => this.GetTable<RbacLinkedPermission>();
    }
}