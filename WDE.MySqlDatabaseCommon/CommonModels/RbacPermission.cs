using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels
{
    [Table(Name = "rbac_permissions")]
    public class RbacPermission : IAuthRbacPermission
    {
        [PrimaryKey]
        [Column(Name = "id")]
        public uint Id { get; set; }

        [Column(Name = "name")] 
        public string Name { get; set; } = "";
    }
}