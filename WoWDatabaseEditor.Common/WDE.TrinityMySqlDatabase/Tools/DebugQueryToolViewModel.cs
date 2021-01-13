using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using LinqToDB.Data;
using LinqToDB.Tools;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.TrinityMySqlDatabase.Services;

namespace WDE.TrinityMySqlDatabase.Tools
{
    public class DebugQueryToolViewModel : BindableBase, ITool, ICodeEditorViewModel
    {
        private readonly IDatabaseLogger databaseLogger;
        public string Title => "Database query debugger";
        private Visibility visibility = Visibility.Hidden;
 
        public ICommand ClearConsole {get;}
 
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
            Text.BeginUpdate();
            Text.Insert(Text.TextLength, $"[{tl}]: {arg2}\n{arg1}");
            Text.EndUpdate();
            ScrollToEnd.Invoke();
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