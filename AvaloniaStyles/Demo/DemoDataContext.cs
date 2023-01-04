using System;
using System.Collections.ObjectModel;

namespace AvaloniaStyles.Demo;

public class DemoDataContext
{
    public SystemThemeOptions CurrentTheme { get; set; }
    public ObservableCollection<SystemThemeOptions> Themes { get; } = new();
    public ObservableCollection<DemoItem> Items { get; } = new(); 
        
    public ObservableCollection<string> Tabs { get; } = new();
        
    public string ActiveTab { get; set; }

    public ExampleEnum EnumValue { get; set; }

    public DemoDataContext()
    {
        EnumValue = ExampleEnum.Cocktail;
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
            
        foreach (var r in Enum.GetValues<SystemThemeOptions>())
            Themes.Add(r);
        CurrentTheme = Themes[0];
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
}