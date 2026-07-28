using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Serilog;
using WDE.Common;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Avalonia.Services.MessageBoxService;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public interface IMainWindowHolder
    {
        Window RootWindow { get; set; }
        Task<T> ShowDialog<T>(Window window);
        Window TopWindow { get; }
        void Show(Window window);
    }

    [AutoRegister]
    [SingleInstance]
    public class MainWindowHolderHolder : IMainWindowHolder
    {
        public Window RootWindow { get; set; } = null!;
        
        private Window? FindTopWindow()
        {
            var windows = ((IClassicDesktopStyleApplicationLifetime?)Application.Current!.ApplicationLifetime)?.Windows;

            if (windows == null)
                throw new Exception("No windows found! Are you running it in web assembly?");
            
            var window = windows.SingleOrDefault(w => w.IsActive);
            if (window is MessageBoxView)
                return window.Owner as Window;
            else if (window is SplashScreenWindow)
                return null;
            else if (window != null)
                return window;

            foreach (var w in windows)
            {
                if (w is MainWindowWithDocking)
                {
                    return w;
                }
            }
            LOG.LogWarning("No window is MainWindowWithDocking. Available windows:");
            foreach (var w in windows)
            {
                LOG.LogWarning($" - {w.Title} ({w.GetType()}");
            }

            return windows.FirstOrDefault(x => x.IsActive) ?? windows.FirstOrDefault();
        }
        
        public Task<T> ShowDialog<T>(Window window)
        {
            var top = FindTopWindow();
            if (top != null)
            {
                if (top.WindowState == WindowState.Minimized)
                    top.WindowState = WindowState.Normal;
                return window.ShowDialog<T>(top);
            }

            throw new Exception("Trying to show dialog without any active window! Are you closing the editor already?");
        }

        public Window TopWindow => FindTopWindow()!;

        public void Show(Window window)
        {
            var top = FindTopWindow();
            if (top != null && top.WindowState == WindowState.Minimized)
                top.WindowState = WindowState.Normal;
            if (top != null)
                window.Show(top);
            else
            {
                LOG.LogWarning("Trying to show a window without any active window! Are you closing the editor already?");
                window.Show();
            }
        }
    }
}