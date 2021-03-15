namespace WDE.Updater.Services
{
    public interface IStandaloneUpdater
    {
        bool Launch();
        void RenameIfNeeded();
    }
}