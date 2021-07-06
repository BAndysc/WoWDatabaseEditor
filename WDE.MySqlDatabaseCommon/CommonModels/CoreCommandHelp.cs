using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels
{
    [Table(Name = "command")]
    public class CoreCommandHelp : ICoreCommandHelp
    {
        [PrimaryKey] 
        [Column(Name = "name")] 
        public string Name { get; set; } = "";
        
        [Column(Name = "help")]
        public string? Help { get; set;}
    }
}