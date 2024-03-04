using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Prism.Ioc;
using WDE.Common.Windows;

namespace WDE.Common.Avalonia.Utils
{
    public class ViewBind
    {
        public static IViewLocator? AppViewLocator { get; set; }
        public static IContainerProvider? ContainerProvider { get; set; }

        public static T ResolveViewModel<T>()
        {
            return ContainerProvider.Resolve<T>();
        }
        
        public static bool TryResolve(object viewModel, out object? view)
        {
            view = null;
            if (AppViewLocator != null && AppViewLocator.TryResolve(viewModel.GetType(), out var viewType, out _))
            {
                Console.WriteLine("Creating view for " + viewModel.GetType().Name + " -> " + viewType.Name);
                view = Activator.CreateInstance(viewType);
            }
            
            return view != null;
        }
        
        public static bool CanResolve(object viewModel)
        {
            return AppViewLocator != null && AppViewLocator.TryResolve(viewModel.GetType(), out _, out _);
        }
    }

    public class ToolbarDataTemplate : IDataTemplate
    {
        public static IDataTemplate Template { get; } = new ToolbarDataTemplate();
        public Control Build(object? param)
        {
            if (ViewBind.AppViewLocator != null && param != null &&
                ViewBind.AppViewLocator.TryResolveToolBar(param.GetType(), out var toolbarType))
            {
                try
                {
                    return (Control)Activator.CreateInstance(toolbarType)!;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return new TextBlock() { Text = e.ToString() };
                }
            }
            return new Control();
        }

        public bool Match(object? data)
        {
            return data is not Control && data is not string;
        }
    }
    
    public class ViewDataTemplate : IDataTemplate
    {
        public static IDataTemplate Template { get; } = new ViewDataTemplate();
        public Control Build(object? param)
        {
            string? fail = null;
            if (ViewBind.AppViewLocator != null && param != null &&
                ViewBind.AppViewLocator.TryResolve(param.GetType(), out var viewType, out fail))
            {
                try
                {
                    return (Control)Activator.CreateInstance(viewType)!;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return new TextBlock() { Text = e.ToString() };
                }
            }
            return new TextBlock()
            {
                Text = "Couldn't find a View for " + param?.GetType().Name + ". " + (fail ?? ""),
                Foreground = Brushes.Red
            };
        }

        public bool Match(object? data)
        {
            return data is not Control && data is not string;
        }
    }
}
