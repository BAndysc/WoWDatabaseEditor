using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia.Utils;
using WDE.Common.CoreVersion;
using WDE.Common.Types;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.Common.Avalonia.Controls;

public class CoreVersionPicker : TemplatedControl
{
    public static readonly StyledProperty<IReadOnlyList<string>?> CoresProperty = AvaloniaProperty.Register<CoreVersionPicker, IReadOnlyList<string>?>(nameof(Cores), defaultBindingMode: BindingMode.TwoWay);

    public ObservableCollection<CoreVersionViewModel> CoresList { get; } = new();
    
    public IReadOnlyList<string>? Cores
    {
        get => GetValue(CoresProperty);
        set => SetValue(CoresProperty, value);
    }

    public CoreVersionPicker()
    {
        var coreVersions = ViewBind.ResolveViewModel<IEnumerable<ICoreVersion>>();
        foreach (var model in coreVersions)
            CoresList.Add(new CoreVersionViewModel(this, model.Tag, model.Icon));
    }

    static CoreVersionPicker()
    {
        CoresProperty.Changed.AddClassHandler<CoreVersionPicker>((picker, e) =>
        {
            if (e.NewValue is not IReadOnlyList<string> cores)
                return;

            foreach (var core in cores)
            {
                var vm = picker.CoresList.FirstOrDefault(x => x.Name == core);
                if (vm == null)
                {
                    vm = new CoreVersionViewModel(picker, core, new ImageUri("Icons/core_unknown.png"));
                    picker.CoresList.Add(vm);
                }
            }
        });
    }
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);         
        var completion = e.NameScope.Get<CompletionComboBox>("CompletionComboBox");
        completion.Items = CoresList;
    }

    public void AddCore(string name)
    {
        if (Cores != null && Cores.Contains(name))
            return;
        
        var cores = Cores?.ToList() ?? new List<string>();
        cores.Add(name);
        SetCurrentValue(CoresProperty, cores);
    }

    public void RemoveCore(string name)
    {
        if (Cores == null || !Cores.Contains(name))
            return;
        
        var cores = Cores.ToList();
        cores.Remove(name);
        SetCurrentValue(CoresProperty, cores);
    }
}

public partial class CoreVersionViewModel : ObservableBase
{
    private readonly CoreVersionPicker parent;

    public CoreVersionViewModel(CoreVersionPicker parent, string name, ImageUri icon)
    {
        this.parent = parent;
        Name = name;
        Icon = icon;
        this.parent.ToObservable(x => x.Cores)
            .SubscribeAction(_ => RaisePropertyChanged(nameof(IsChecked)));
    }

    public bool IsChecked
    {
        get => parent.Cores?.Contains(Name) ?? false;
        set
        {
            if (value)
                parent.AddCore(Name);
            else
                parent.RemoveCore(Name);
        }
    }
    public string Name { get; }
    public ImageUri Icon { get; }

    public override string ToString() => Name;
}