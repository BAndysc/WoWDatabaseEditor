namespace WDE.Common.History
{
    public interface IHistoryAction
    {
        void Undo();
        void Redo();

        string GetDescription();
    }
}