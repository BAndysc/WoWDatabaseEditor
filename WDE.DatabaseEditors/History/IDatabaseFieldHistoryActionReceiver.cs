using WDE.Common.History;

namespace WDE.DatabaseEditors.History
{
    public interface IDatabaseFieldHistoryActionReceiver
    {
        void RegisterAction(IHistoryAction action);
    }
}