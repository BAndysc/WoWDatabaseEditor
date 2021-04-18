namespace WDE.Updater.Services
{
    public interface IStandaloneUpdater
    {
        bool Launch();
        bool HasPendingUpdate();
        void RenameIfNeeded();
    }
}