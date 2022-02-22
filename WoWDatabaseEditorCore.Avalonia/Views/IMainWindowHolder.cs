using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public interface IMainWindowHolder
    {
        Window Window { get; set; }
        Stack<Window> WindowStack { get; }
        Task<T> ShowDialog<T>(Window window);
    }

    [AutoRegister]
    [SingleInstance]
    public class MainWindowHolderHolder : IMainWindowHolder
    {
        private Window window = null!;

        public Window Window
        {
            get => window;
            set
            {
                window = value;
                WindowStack.Push(value);
            }
        }

        public Stack<Window> WindowStack { get; } = new();

        public Task<T> ShowDialog<T>(Window window)
        {
            var parent = WindowStack.Peek();
            window.Closed += (sender, args) =>
            {
                Debug.Assert(WindowStack.Peek() == window);
                WindowStack.Pop();
            };
            WindowStack.Push(window);
            return window.ShowDialog<T>(parent);
        }
    }
}