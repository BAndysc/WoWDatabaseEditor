using System;
using System.ComponentModel;
using System.Windows.Input;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.ViewModels
{
    [AutoRegister]
    public class AboutViewModel : IDocument
    {
        private readonly IApplicationVersion applicationVersion;

        public AboutViewModel(IApplicationVersion applicationVersion)
        {
            this.applicationVersion = applicationVersion;
        }

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