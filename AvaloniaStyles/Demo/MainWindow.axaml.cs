using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls;

namespace AvaloniaStyles.Demo
{
    public class MainWindow : ExtendedWindow
    {
        public SystemThemeOptions CurrentTheme { get; set; }
        public ObservableCollection<SystemThemeOptions> Themes { get; } = new();
        public ObservableCollection<DemoItem> Items { get; } = new(); 
        
        public ObservableCollection<string> Tabs { get; } = new();
        
        public string ActiveTab { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.AttachDevTools();
            DataContext = this;
            Items.Add(new DemoItem(true, "Brian", 213));
            Items.Add(new DemoItem(false, "Justin", 4125));
            Items.Add(new DemoItem(true, "Ted", 7548));
            Items.Add(new DemoItem(true, "Emmet", 43422));
            Items.Add(new DemoItem(false, "Michael", 94));
            
            Tabs.Add("New file");
            Tabs.Add("Untitled");
            Tabs.Add("My document");
            Tabs.Add("Click me");

            ActiveTab = Tabs[1];
            
            Themes.AddRange(Enum.GetValues<SystemThemeOptions>());
            CurrentTheme = Themes[0];
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public class DemoItem
        {
            public bool Bool { get; set; }
            public string Name { get; }
            public int Number { get; }

            public DemoItem(bool b, string name, int number)
            {
                Bool = b;
                Name = name;
                Number = number;
            }
        }

        private void ShowDemoDialog(object? sender, RoutedEventArgs e)
        {
            new MessageBox().ShowDialog(this);
        }
    }
}