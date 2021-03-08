namespace WDE.DatabaseEditors.History
{
    public interface IDbTableHistoryActionSource
    {
        void RegisterActionReceiver(IDbFieldHistoryActionReceiver receiver);
        void UnregisterActionReceiver();
    }
}