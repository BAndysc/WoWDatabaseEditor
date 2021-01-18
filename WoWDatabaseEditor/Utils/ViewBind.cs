using System;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using WDE.Common.Windows;

namespace WoWDatabaseEditor.Utils
{
    public class ViewBind
    {
        public static readonly DependencyProperty ModelProperty = DependencyProperty.RegisterAttached("Model",
            typeof(object),
            typeof(ViewBind),
            new PropertyMetadata(null, OnModelChanged));

        public static object GetModel(DependencyObject element)
        {
            return element.GetValue(ModelProperty);
        }

        public static void SetModel(DependencyObject element, object value)
        {
            element.SetValue(ModelProperty, value);
        }

        private static void OnModelChanged(DependencyObject targetLocation, DependencyPropertyChangedEventArgs args)
        {
            if (args.OldValue == args.NewValue)
                return;

            var locator = App.GlobalContainer?.Resolve(typeof(IViewLocator)) as IViewLocator;
            
            if (locator!.TryResolve(args.NewValue?.GetType(), out var viewType))
            {
                object? view = Activator.CreateInstance(viewType);
                SetContentProperty(targetLocation, view);
            }
            else
                SetContentProperty(targetLocation, null);
        }

        private static bool SetContentProperty(object targetLocation, object? view)
        {
            try
            {
                Type? type = targetLocation.GetType();
                ContentPropertyAttribute? contentProperty = type.GetCustomAttributes(typeof(ContentPropertyAttribute), true)
                    .OfType<ContentPropertyAttribute>()
                    .FirstOrDefault() ?? new ContentPropertyAttribute("Content");

                type.GetProperty(contentProperty?.Name ?? new ContentPropertyAttribute("Content").Name)
                    ?.SetValue(targetLocation, view, null);

                return true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}