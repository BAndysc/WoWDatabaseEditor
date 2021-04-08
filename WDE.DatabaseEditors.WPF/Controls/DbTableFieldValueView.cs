using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace WDE.DatabaseEditors.WPF.Controls
{
    public class DbTableFieldValueView : Control
    {
        public static readonly DependencyProperty ShowModifiedIconProperty = DependencyProperty.Register(
            nameof(ShowModifiedIcon),
            typeof(bool),
            typeof(DbTableFieldValueView), new PropertyMetadata(true));

        public bool ShowModifiedIcon
        {
            get => (bool) GetValue(ShowModifiedIconProperty);
            set => SetValue(ShowModifiedIconProperty, value);
        }
        
        public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.Register(
            nameof(IsModified),
            typeof(bool),
            typeof(DbTableFieldValueView), new PropertyMetadata(false));

        public bool IsModified
        {
            get => (bool) GetValue(IsModifiedProperty);
            set => SetValue(IsModifiedProperty, value);
        }
    }
}