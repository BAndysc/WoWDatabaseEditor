using Prism.Commands;
using Prism.Mvvm;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WDE.Common.Managers;

namespace WoWDatabaseEditor.Managers.ViewModels
{
    internal class ToolWindow : BindableBase, ITool
    {
        public string Title { get; private set; }

        public ContentControl Content { get; private set; }

        public bool CanClose => false;

        public ICommand CloseCommand { get; private set; }

        private Visibility _visibility;
        public Visibility Visibility
        {
            get => _visibility;
            set => SetProperty(ref _visibility, value);
        }

        public ToolWindow(string title, ContentControl content)
        {
            Title = title;
            Content = content;
            Visibility = System.Windows.Visibility.Visible;
            CloseCommand = new DelegateCommand(() => { });
        }
    }
}
