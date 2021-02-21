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
        
        private static object OnModelChanged(IAvaloniaObject targetLocation, object viewModel)
        {
            if (AppViewLocator != null && AppViewLocator.TryResolve(viewModel.GetType(), out var viewType))
            {
                object? view = Activator.CreateInstance(viewType);
                SetContentProperty(targetLocation, view);
            }
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