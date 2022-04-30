using System;
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
            window.Closed += WindowOnClosed;
            WindowStack.Push(window);
            return window.ShowDialog<T>(parent);
        }

        private void WindowOnClosed(object? sender, EventArgs e)
        {
            var window = (sender as Window)!;
            if (!ReferenceEquals(WindowStack.Peek(), window))
            {
                Debug.Assert(false, "Dialog window closed in wrong order?!!");
                var stackTrace = new StackTrace(true);
                Console.WriteLine("Dialog window closed in wrong order?!\n" + stackTrace);
            }
            WindowStack.Pop();
            window.Closed -= WindowOnClosed;
        }
    }
}