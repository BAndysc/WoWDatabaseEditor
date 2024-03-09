
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;


public class JsonPlayerChoice : IPlayerChoice
{
    
    
    public int ChoiceId { get; set; }
    
    
    public string Question { get; set; } = "";
}


public class JsonPlayerChoiceResponse : IPlayerChoiceResponse
{
    
    
    public int ResponseId { get; set; }

    
    public int ChoiceId { get; set; }

    
    public string Header { get; set; } = "";
    
    
    public string Answer { get; set; } = "";
}