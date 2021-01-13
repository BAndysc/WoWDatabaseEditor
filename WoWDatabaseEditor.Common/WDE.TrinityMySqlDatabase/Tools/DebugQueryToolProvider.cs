using WDE.Common.Managers;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.TrinityMySqlDatabase.Services;

namespace WDE.TrinityMySqlDatabase.Tools
{
    [AutoRegister]
    public class DebugQueryToolProvider : IToolProvider
    {
        private readonly IDatabaseLogger databaseLogger;
        public bool AllowMultiple => false;
        public string Name => "Database query debugger";
        public bool CanOpenOnStart => false;

        public DebugQueryToolProvider(IDatabaseLogger databaseLogger)
        {
            this.databaseLogger = databaseLogger;
        }
        
        public ITool Provide()
        {
            return new DebugQueryToolViewModel(databaseLogger);
        }
    }
}