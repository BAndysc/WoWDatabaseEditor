namespace WDE.DatabaseEditors.History
{
    public interface IDatabaseTableHistoryActionSource
    {
        void RegisterActionReceiver(IDatabaseFieldHistoryActionReceiver receiver);
        void UnregisterActionReceiver();
    }
}