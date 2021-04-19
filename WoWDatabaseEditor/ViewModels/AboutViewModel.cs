﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.CoreVersion;

namespace WoWDatabaseEditorCore.ViewModels
{
    [AutoRegister]
    public class AboutViewModel : IDocument
    {
        private readonly IApplicationVersion applicationVersion;

        public AboutViewModel(IApplicationVersion applicationVersion,
            IDatabaseProvider databaseProvider, 
            IDbcStore dbcStore,
            IConfigureService settings,
            ICurrentCoreVersion coreVersion,
            IRemoteConnectorService remoteConnectorService)
        {
            this.applicationVersion = applicationVersion;
            
            ConfigurationChecks.Add(new ConfigurationCheckup(true, 
                "Core version compatibility mode", 
                "WoW Database Editor supports multiple world of warcraft server cores. In order to achieve maximum compatibility, choose version that matches best.\nYou are using: " + coreVersion.Current.FriendlyName + " compatibility mode now."));
            
            ConfigurationChecks.Add(new ConfigurationCheckup(dbcStore.IsConfigured, 
                "DBC settings", 
                "DBC is DataBaseClient files provided with WoW client. Those contain a lot of useful stuff for scripting like spells data. For maximum features you have to provide DBC files path. All WoW servers require those files to work so if you have working core, you must have DBC files already."));
            
            ConfigurationChecks.Add(new ConfigurationCheckup(databaseProvider.IsConnected, 
                "Database connection", 
                "WoW Database Editor is database editor by design. It stores all data and loads things from wow database. Therefore to activate all features you have to provide wow compatible database connection settings."));

            ConfigurationChecks.Add(new ConfigurationCheckup(remoteConnectorService.IsConnected, 
                "Remote SOAP connection", 
                "WDE can invoke reload commands for you for faster work. To enable that, you have to enable SOAP connection in your worldserver configuration and provide details in the settings."));

            AllConfigured = ConfigurationChecks.All(s => s.Fulfilled);

            OpenSettingsCommand = new DelegateCommand(settings.ShowSettings);
        }

        public ICommand OpenSettingsCommand { get; }
        public bool AllConfigured { get; }
        public ObservableCollection<ConfigurationCheckup> ConfigurationChecks { get; } = new();
        public int BuildVersion => applicationVersion.BuildVersion;
        public string Branch => applicationVersion.Branch;
        public string CommitHash => applicationVersion.CommitHash;
        public bool VersionKnown => applicationVersion.VersionKnown;
        public string ReleaseData => $"WoWDatabaseEditor, branch: {Branch}, build: {BuildVersion}, commit: {CommitHash}";
        
        public string Title { get; } = "About";
        public ICommand Undo { get; } = new DisabledCommand();
        public ICommand Redo { get; } = new DisabledCommand();
        public ICommand Copy { get; } = new DisabledCommand();
        public ICommand Cut { get; } = new DisabledCommand();
        public ICommand Paste { get; } = new DisabledCommand();
        public ICommand Save { get; } = new DisabledCommand();
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