using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using WDE.Common.Menu;
using WDE.Common.Windows;

namespace WDE.Common.Avalonia.Utils
{
    public class ViewBind
    {
        public static IViewLocator? AppViewLocator { get; set; }
        
        public static readonly AvaloniaProperty ModelProperty = AvaloniaProperty.RegisterAttached<ContentControl, object>("Model",
            typeof(ViewBind),coerce: OnModelChanged);
        
        public static object GetModel(ContentControl control) => control.GetValue(ModelProperty);
        public static void SetModel(ContentControl control, object value) => control.SetValue(ModelProperty, value);
        
        private static object OnModelChanged(IAvaloniaObject targetLocation, object viewModel)
        {
            if (AppViewLocator != null && AppViewLocator.TryResolve(viewModel.GetType(), out var viewType))
            {
                object? view = Activator.CreateInstance(viewType);
                SetContentProperty(targetLocation, view);
            }
            else
                SetContentProperty(targetLocation, null);

            return viewModel;
        }

        private static bool SetContentProperty(IAvaloniaObject targetLocation, object? view)
        {
            try
            {
                if (view != null)
                {
                    Type? type = targetLocation.GetType();
                    type.GetProperty("Content")
                        ?.SetValue(targetLocation, view);
                }

                return true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}