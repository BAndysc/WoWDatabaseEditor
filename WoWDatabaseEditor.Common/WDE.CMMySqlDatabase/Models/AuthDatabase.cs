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
//         public ITable<RbacPermission> RbacPermissions => GetTable<RbacPermission>();
//         public ITable<RbacLinkedPermission> RbacLinkedPermissions => GetTable<RbacLinkedPermission>();
    }
}