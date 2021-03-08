using WDE.Common.History;

namespace WDE.DatabaseEditors.History
{
    public interface IDbFieldHistoryActionReceiver
    {
        void RegisterAction(IHistoryAction action);
    }
}