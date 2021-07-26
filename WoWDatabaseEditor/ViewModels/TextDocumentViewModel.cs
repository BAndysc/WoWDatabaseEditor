using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Mvvm;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.ViewModels
{
    [AutoRegister]
    public class TextDocumentViewModel : BindableBase, IDocument
    {
        public TextDocumentViewModel(IWindowManager windowManager,
            INativeTextDocument nativeTextDocument)
        {
            Extension = "txt";
            Title = "New file";
            document = nativeTextDocument;

            SaveCommand = new AsyncAutoCommand(async () =>
            {
                var path = await windowManager.ShowSaveFileDialog($"{Extension} file|{Extension}|All files|*");
                if (path != null)
                    await File.WriteAllTextAsync(path, document.ToString());
            });
        }

        public TextDocumentViewModel Set(string title, string content, string extension = "txt")
        {
            Title = title;
            Extension = extension;
            Icon = new ImageUri($"icons/document_{extension.ToLower()}.png");
            document.FromString(content);
            return this;
        }
        
        public void Dispose()
        {
        }

        private INativeTextDocument document;
        public INativeTextDocument Document
        {
            get => document;
            set => SetProperty(ref document, value);
        }
        public string Extension { get; set; }
        public string Title { get; set; }
        public ICommand SaveCommand { get; }
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save => AlwaysDisabledCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History => null;
        public ImageUri? Icon { get; set; }
    }
}