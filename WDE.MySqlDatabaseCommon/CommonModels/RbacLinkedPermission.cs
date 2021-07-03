using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels
{
    [Table(Name = "rbac_linked_permissions")]
    public class RbacLinkedPermission : IAuthRbacLinkedPermission
    {
        [PrimaryKey]
        [Column(Name = "id")]
        public uint Id { get; set; }
        
        [PrimaryKey]
        [Column(Name = "linkedId")]
        public uint LinkedId { get; set; }
    }
}