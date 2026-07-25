using WDE.Common.Services;

namespace WDE.DbScriptsEditor.Models
{
    // `reload dbscripts_on_<x>` sent to the running server after applying changes.
    public class DbScriptReloadRemoteCommand : IRemoteCommand
    {
        private readonly string tableName;

        public DbScriptReloadRemoteCommand(string tableName)
        {
            this.tableName = tableName;
        }

        public string GenerateCommand() => $"reload {tableName}";

        public RemoteCommandPriority Priority => RemoteCommandPriority.VeryFirst;

        public bool TryMerge(IRemoteCommand other, out IRemoteCommand? mergedCommand)
        {
            if (other is DbScriptReloadRemoteCommand o && o.tableName == tableName)
            {
                mergedCommand = this;
                return true;
            }
            mergedCommand = null;
            return false;
        }
    }
}
