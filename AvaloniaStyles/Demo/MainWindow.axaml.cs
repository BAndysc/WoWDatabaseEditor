using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls;

namespace AvaloniaStyles.Demo
{
    public partial class MainWindow : ExtendedWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.AttachDevTools();
            DataContext = new DemoDataContext();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    [Flags]
    public enum ExampleEnum
    {
        Food = 1,
        Drink = 2,
        Cocktail = 4,
        Car = 8,
        Vehicle = 16,
        House = 32,
    }
}