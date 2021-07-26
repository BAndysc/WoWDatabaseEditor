using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using WDE.Common.Menu;
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
    
    public class ToolBarBind
    {
        public static readonly AvaloniaProperty ModelProperty = AvaloniaProperty.RegisterAttached<Control, object>("Model",
            typeof(ToolBarBind),coerce: OnModelChanged);
        
        public static object GetModel(Control control) => control.GetValue(ModelProperty);
        public static void SetModel(Control control, object value) => control.SetValue(ModelProperty, value);
        
        public static bool TryResolveToolBar(object viewModel, out object? toolbar)
        {
            toolbar = null;
            if (ViewBind.AppViewLocator != null &&
                ViewBind.AppViewLocator.TryResolveToolBar(viewModel.GetType(), out var toolbarType))
            {
                try
                {
                    toolbar = Activator.CreateInstance(toolbarType);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    toolbar = new TextBlock() { Text = e.ToString() };
                }
            }
            
            return toolbar != null;
        }

        private static object OnModelChanged(IAvaloniaObject targetLocation, object viewModel)
        {
            if (TryResolveToolBar(viewModel, out var view))
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
}