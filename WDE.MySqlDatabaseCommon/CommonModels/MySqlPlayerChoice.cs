using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "playerchoice")]
public class MySqlPlayerChoice : IPlayerChoice
{
    [PrimaryKey]
    [Column(Name = "ChoiceId")]
    public int ChoiceId { get; set; }
    
    [Column(Name = "Question")]
    public string Question { get; set; } = "";
}

[Table(Name = "playerchoice_response")]
public class MySqlPlayerChoiceResponse : IPlayerChoiceResponse
{
    [PrimaryKey]
    [Column(Name = "ResponseId")]
    public int ResponseId { get; set; }

    [Column(Name = "ChoiceId")]
    public int ChoiceId { get; set; }

    [Column(Name = "Header")]
    public string Header { get; set; } = "";
    
    [Column(Name = "Answer")]
    public string Answer { get; set; } = "";
}