using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.TrinityMySqlDatabase.Services;

namespace WDE.TrinityMySqlDatabase.Tools
{
    [AutoRegister]
    [SingleInstance]
    public class DebugQueryToolViewModel : BindableBase, ITool, ICodeEditorViewModel
    {
        private readonly IDatabaseLogger databaseLogger;
        public string Title => "Database query debugger";
        public string UniqueId => "trinity_database_query_debugger";
        private Visibility visibility = Visibility.Hidden;
 
        public ICommand ClearConsole {get;}
 
        public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Bottom;
        public bool OpenOnStart => false;

        public TextDocument Text { get; } = new TextDocument();
        public Visibility Visibility
        {
            get => visibility; 
            set
            {
                if (value == visibility)
                    return;
                if (value == Visibility.Visible)
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
        
        public DebugQueryToolViewModel(IDatabaseLogger databaseLogger)
        {
            this.databaseLogger = databaseLogger;
            
            ClearConsole = new DelegateCommand(() =>
            {
                Clear.Invoke();
                ScrollToEnd.Invoke();      
            });
        }

        private void OnTrace(string? arg1, string? arg2, TraceLevel tl)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Text.BeginUpdate();
                Text.Insert(Text.TextLength, $"[{tl}]: {arg2}\n{arg1}");
                Text.EndUpdate();
                ScrollToEnd.Invoke();
            });
        }

        public event Action ScrollToEnd = delegate {};
        public event Action Clear = delegate {};
    }

    internal interface ICodeEditorViewModel
    {
        event Action ScrollToEnd;
        event Action Clear;
    }
}