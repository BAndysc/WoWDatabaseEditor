using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using WDE.Common.Windows;

namespace WDE.Common.Avalonia.Utils
{
    public class ViewBind
    {
        public static IViewLocator? AppViewLocator { get; set; }
        
        public static readonly AvaloniaProperty ModelProperty = AvaloniaProperty.RegisterAttached<Control, object>("Model",
            typeof(ViewBind),coerce: OnModelChanged);
        
        public static object GetModel(Control control) => control.GetValue(ModelProperty);
        public static void SetModel(Control control, object value) => control.SetValue(ModelProperty, value);
        
        public static bool TryResolve(object viewModel, out object? view)
        {
            view = null;
            if (AppViewLocator != null && AppViewLocator.TryResolve(viewModel.GetType(), out var viewType))
                view = Activator.CreateInstance(viewType);
            
            return view != null;
        }
        
        private static object OnModelChanged(IAvaloniaObject targetLocation, object viewModel)
        {
            if (TryResolve(viewModel, out var view))
                SetContentProperty(targetLocation, view);
            else
                SetContentProperty(targetLocation, new Panel());

            return viewModel;
        }

        private static void SetContentProperty(IAvaloniaObject targetLocation, object? view)
        {
            if (view != null && targetLocation != null)
            {
                Type? type = targetLocation.GetType();
                type.GetProperty("Content")?.SetValue(targetLocation, view);
            }
        }
    }

    public class ToolbarDataTemplate : IDataTemplate
    {
        public static IDataTemplate Template { get; } = new ToolbarDataTemplate();
        public IControl Build(object param)
        {
            if (ViewBind.AppViewLocator != null && param != null &&
                ViewBind.AppViewLocator.TryResolveToolBar(param.GetType(), out var toolbarType))
            {
                try
                {
                    return (IControl)Activator.CreateInstance(toolbarType)!;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return new TextBlock() { Text = e.ToString() };
                }
            }
            return new Control();
        }

        public bool Match(object data)
        {
            return data is not IControl && data is not string;
        }
    }
}