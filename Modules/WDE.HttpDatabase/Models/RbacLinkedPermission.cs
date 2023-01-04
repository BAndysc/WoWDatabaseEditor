
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class RbacLinkedPermission : IAuthRbacLinkedPermission
    {
        
        public uint Id { get; set; }
        
        
        public uint LinkedId { get; set; }
    }
}