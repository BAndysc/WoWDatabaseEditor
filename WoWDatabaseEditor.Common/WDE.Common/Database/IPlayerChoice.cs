namespace WDE.Common.Database;

public interface IPlayerChoice
{
    int ChoiceId { get; }
    string Question { get; }
}

public interface IPlayerChoiceResponse
{
    int ResponseId { get; }
    int ChoiceId { get; }
    string Header { get; }
    string Answer { get; }
}