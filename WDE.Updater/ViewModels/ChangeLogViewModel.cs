using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.Updater.Models;

namespace WDE.Updater.ViewModels
{
    public class ChangeLogViewModel : IDocument
    {
        public List<ChangeLogEntry> Changes { get; }

        public ChangeLogViewModel(List<ChangeLogEntry> changelog)
        {
            Changes = changelog;
            var start = changelog[^1].Date.ToString("m");
            var end = changelog[0].Date.ToString("m");
            Title = $"Changelog {start} - {end}";
        }
        
        public void Dispose() { }
        public event PropertyChangedEventHandler? PropertyChanged;
        public string Title { get; }
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public IAsyncCommand Save => AlwaysDisabledAsyncCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History => null;
    }
}