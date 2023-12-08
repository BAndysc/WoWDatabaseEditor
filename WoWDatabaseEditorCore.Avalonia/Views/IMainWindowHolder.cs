﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public interface IMainWindowHolder
    {
        Window RootWindow { get; set; }
        Task<T> ShowDialog<T>(Window window);
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
            
            return windows.SingleOrDefault(w => w.IsActive) ?? windows.SingleOrDefault(w => w is MainWindowWithDocking);
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

        public void Show(Window window)
        {
            var top = FindTopWindow();
            if (top != null && top.WindowState == WindowState.Minimized)
                top.WindowState = WindowState.Normal;
            if (top != null)
                window.Show(top);
            else
            {
                Console.WriteLine("Trying to show a window without any active window! Are you closing the editor already?");
                window.Show();
            }
        }
    }
}