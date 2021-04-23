using System.Windows;
using System.Windows.Controls;

namespace WDE.DatabaseEditors.WPF.Controls
{
    public class DbTableFieldView : Control
    {
        public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.Register(
            nameof(IsModified),
            typeof(bool),
            typeof(DbTableFieldView), new PropertyMetadata(false));

        public bool IsModified
        {
            get => (bool) GetValue(IsModifiedProperty);
            set => SetValue(IsModifiedProperty, value);
        }
        
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(DbTableFieldView), new PropertyMetadata(""));

        public string Title
        {
            get => (string) GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
    }
}