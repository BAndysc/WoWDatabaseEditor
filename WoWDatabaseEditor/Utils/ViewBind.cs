using System;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

namespace WoWDatabaseEditor.Utils
{
    public class ViewBind
    {
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.RegisterAttached(
                "Model",
                typeof(object),
                typeof(ViewBind),
                new PropertyMetadata(null, 
                    OnModelChanged)
            );
         
        public static object GetModel(DependencyObject element)
        {
            return element.GetValue(ModelProperty);
        }

        public static void SetModel(DependencyObject element, object value)
        {
            element.SetValue(ModelProperty, value);
        }
         
        static void OnModelChanged(DependencyObject targetLocation, DependencyPropertyChangedEventArgs args) {
            if (args.OldValue == args.NewValue) {
                return;
            }

            if (args.NewValue?.GetType().AssemblyQualifiedName != null) {
                var vmType = args.NewValue.GetType();
                var viewType = Type.GetType(vmType.AssemblyQualifiedName!.Replace("ViewModel", "View"));
                var view = (viewType == null) ? null : Activator.CreateInstance(viewType);
                SetContentProperty(targetLocation, view);
            }
            else {
                SetContentProperty(targetLocation, args.NewValue);
            }
        }
        
        static bool SetContentProperty(object targetLocation, object? view) {
            try {
                var type = targetLocation.GetType();
                var contentProperty = type.GetCustomAttributes(typeof(ContentPropertyAttribute), true)
                    .OfType<ContentPropertyAttribute>()
                    .FirstOrDefault() ?? new ContentPropertyAttribute("Content");

                type.GetProperty(contentProperty?.Name ?? new ContentPropertyAttribute("Content").Name)
                    ?.SetValue(targetLocation, view, null);

                return true;
            }
            catch(Exception e)
            {
                throw e;
            }
        }
    }
}