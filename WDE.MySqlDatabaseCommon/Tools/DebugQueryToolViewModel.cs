using System;
using System.Diagnostics;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Services;

namespace WDE.MySqlDatabaseCommon.Tools
{
    [AutoRegister]
    [SingleInstance]
    public class DebugQueryToolViewModel : BindableBase, ITool, ICodeEditorViewModel
    {
        private readonly IDatabaseLogger databaseLogger;
        private readonly IMainThread mainThread;
        public string Title => "Database query debugger";
        public string UniqueId => "database_query_debugger";
        private bool visibility = false;
 
        public ICommand ClearConsole {get;}
 
        public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Bottom;
        public bool OpenOnStart => false;

        public INativeTextDocument Text { get; }
        public bool Visibility
        {
            get => visibility; 
            set
            {
                if (value == visibility)
                    return;
                if (value)
                {
                    databaseLogger.OnLog += OnTrace;
                }
                else
                {
                    databaseLogger.OnLog -= OnTrace;
                }
                SetProperty(ref visibility, value);
            }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
        
        public DebugQueryToolViewModel(IDatabaseLogger databaseLogger, IMainThread mainThread, INativeTextDocument document)
        {
            this.databaseLogger = databaseLogger;
            this.mainThread = mainThread;
            this.Text = document;

            ClearConsole = new DelegateCommand(() =>
            {
                Clear.Invoke();
                ScrollToEnd.Invoke();      
            });
        }

        private void OnTrace(string? arg1, string? arg2, TraceLevel tl, QueryType queryType)
        {
            mainThread.Dispatch(() =>
            {
                Text.Append($"[{tl}]: {arg2}\n{arg1}\n");
                ScrollToEnd.Invoke();
            });
        }

        public event Action ScrollToEnd = delegate {};
        public event Action Clear = delegate {};
    }

    public interface ICodeEditorViewModel
    {
        event Action ScrollToEnd;
        event Action Clear;
    }
}