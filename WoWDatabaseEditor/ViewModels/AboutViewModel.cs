using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Factories.Http;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.CoreVersion;
using WoWDatabaseEditorCore.Services;

namespace WoWDatabaseEditorCore.ViewModels
{
    [AutoRegister]
    public class AboutViewModel : IDocument, IUserAgentPart
    {
        private readonly IApplicationVersion applicationVersion;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IDbcStore dbcStore;
        private readonly IRemoteConnectorService remoteConnectorService;

        public AboutViewModel(IApplicationVersion applicationVersion,
            IDatabaseProvider databaseProvider,
            IDbcStore dbcStore,
            Lazy<IConfigureService> settings,
            ICurrentCoreVersion coreVersion,
            IProgramNameService programNameService,
            IRemoteConnectorService remoteConnectorService,
            ISourceCodePathService sourceCodePathService)
        {
            this.applicationVersion = applicationVersion;
            this.databaseProvider = databaseProvider;
            this.dbcStore = dbcStore;
            this.remoteConnectorService = remoteConnectorService;
            ProgramTitle = programNameService.Title;
            ProgramName = coreVersion.Current.EditorTitle;

            ConfigurationChecks.Add(new ConfigurationCheckup(coreVersion.Current is not UnspecifiedCoreVersion,
                "Core version compatibility mode",
                "The editor supports multiple server cores. In order to achieve maximum compatibility, choose version that matches best.\nYou are using: " + coreVersion.Current.FriendlyName + " compatibility mode now."));

            ConfigurationChecks.Add(new ConfigurationCheckup(dbcStore.IsConfigured,
                "DBC settings",
                "DBC is DataBaseClient files provided with the game client. Those contain a lot of useful stuff for scripting like spells data. For maximum features you have to provide DBC files path. All servers require those files to work so if you have working core, you must have DBC files already."));

            ConfigurationChecks.Add(new ConfigurationCheckup(databaseProvider.IsConnected,
                "Database connection",
                "The editor is a database editor by design. It stores all data and loads things from the server database. Therefore to activate all features you have to provide compatible database connection settings."));

            ConfigurationChecks.Add(new ConfigurationCheckup(remoteConnectorService.HasValidSettings,
                "Remote connection",
                "WDE can invoke reload commands for you for faster work. To enable that, you have to enable remote connection in your worldserver configuration and provide details in the settings."));

            ConfigurationChecks.Add(new ConfigurationCheckup(sourceCodePathService.SourceCodePaths.Count > 0,
                "Source code integration",
                "WDE can integrate with source code of your server. Find Anywhere can search in the source code."));

            AllConfigured = ConfigurationChecks.All(s => s.Fulfilled);

            OpenSettingsCommand = new DelegateCommand(() => settings.Value.ShowSettings());
        }

        public ICommand OpenSettingsCommand { get; }
        public bool AllConfigured { get; }
        public ObservableCollection<ConfigurationCheckup> ConfigurationChecks { get; } = new();
        public string ProgramTitle { get; }
        public string ProgramName { get; }
        public int BuildVersion => applicationVersion.BuildVersion;
        public string Branch => applicationVersion.Branch;
        public string CommitHash => applicationVersion.CommitHash;
        public bool VersionKnown => applicationVersion.VersionKnown;
        public string ReleaseData => $"{ProgramName}, branch: {Branch}, build: {BuildVersion}, commit: {CommitHash}";

        public ImageUri? Icon => new ImageUri("Icons/wde_icon.png");
        public string Title { get; } = "About";
        public ICommand Undo { get; } = new DisabledCommand();
        public ICommand Redo { get; } = new DisabledCommand();
        public ICommand Copy { get; } = new DisabledCommand();
        public ICommand Cut { get; } = new DisabledCommand();
        public ICommand Paste { get; } = new DisabledCommand();
        public IAsyncCommand Save { get; } = AlwaysDisabledAsyncCommand.Command;
        public AsyncAwaitBestPractices.MVVM.IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        public bool IsModified { get; } = false;
        public IHistoryManager? History { get; } = null;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void Dispose()
        {
        }

        public class ConfigurationCheckup
        {
            public bool Fulfilled { get; }
            public string Title { get; }
            public string Description { get; }

            public ConfigurationCheckup(bool fulfilled, string title, string description)
            {
                Fulfilled = fulfilled;
                Title = title;
                Description = description;
            }
        }

        public string Part => $"(DBC: {BoolToString(dbcStore.IsConfigured)}, DB: {BoolToString(databaseProvider.IsConnected)}, SOAP: {BoolToString(remoteConnectorService.IsConnected)})";

        private string BoolToString(bool b)
        {
            return b ? "Yes" : "No";
        }
    }

    public class DisabledCommand : ICommand
    {
        public bool CanExecute(object? parameter)
        {
            return false;
        }

        public void Execute(object? parameter)
        {
            throw new NotImplementedException();
        }

        public event EventHandler? CanExecuteChanged;
    }
}