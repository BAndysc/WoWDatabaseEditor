using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Styling;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Types;
using WDE.DatabaseEditors.CustomCommands;

namespace WDE.DatabaseDefinitionEditor.Views.Controls;

public class DatabaseTableCommandCompletionBox : CompletionComboBox
{
    protected override Type StyleKeyOverride => typeof(CompletionComboBox);

    public string CommandId
    {
        get => (SelectedItem as DatabaseTableCommandViewModel)?.CommandId ?? "";
        set
        {
            if ((SelectedItem as DatabaseTableCommandViewModel)?.CommandId != value)
            {
                var item = TryFindCommandById(value);
                if (item != null)
                    SelectedItem = item;
                else
                {
                    Populate();
                    PopulateAndTryMatchCommand(value);
                }
            }
        }
    }

    private DatabaseTableCommandViewModel? TryFindCommandById(string commandId)
    {
        return Items?.Cast<DatabaseTableCommandViewModel?>()
            .FirstOrDefault(x => x != null && x.CommandId == commandId);
    }
    
    static DatabaseTableCommandCompletionBox()
    {
        IsDropDownOpenProperty.Changed.AddClassHandler<DatabaseTableCommandCompletionBox>((cb, x) =>
        {
            if (cb.IsDropDownOpen)
                cb.Populate();
        });
        SelectedItemProperty.Changed.AddClassHandler<DatabaseTableCommandCompletionBox>((cb, x) =>
        {
            var old = x.OldValue as DatabaseTableCommandViewModel;
            var @new = x.NewValue as DatabaseTableCommandViewModel;
            cb.RaisePropertyChanged(CommandIdProperty, old?.CommandId ?? "",  @new?.CommandId ?? "");
        });
    }

    public static readonly DirectProperty<DatabaseTableCommandCompletionBox, string> CommandIdProperty = AvaloniaProperty.RegisterDirect<DatabaseTableCommandCompletionBox, string>(nameof(CommandId), o => o.CommandId, (o, v) => o.CommandId = v, defaultBindingMode: BindingMode.TwoWay);
    
    private IList<DatabaseTableCommandViewModel> GetCommands()
    {
        var commands = ViewBind.ResolveViewModel<IEnumerable<IDatabaseTableCommand>>();
        return commands.Select(c => new DatabaseTableCommandViewModel(c.Name, c.CommandId, c.Icon)).ToList();
    }
    
    private void Populate()
    {
        var columns = GetCommands();
        Items = columns;
    }
    
    private void PopulateAndTryMatchCommand(string value)
    {
        Populate();
        
        var item = TryFindCommandById(value);
        if (item != null)
            SelectedItem = item;
        else
            SelectedItem = new DatabaseTableCommandViewModel("(unknown)", value, default);
    }

    public DatabaseTableCommandCompletionBox()
    {
        OnEnterPressed += (_, e) =>
        {
            if (e.SelectedItem == null)
            {
                CommandId = e.SearchText;
                e.Handled = true;
            }
        };
        this.GetResource<IDataTemplate>("DatabaseTableCommandDataTemplate", null!, out var template);
        ItemTemplate = ButtonItemTemplate = template;
    }
}

internal class DatabaseTableCommandViewModel
{
    public DatabaseTableCommandViewModel(string name, string commandId, ImageUri icon)
    {
        Name = name;
        CommandId = commandId;
        Icon = icon;
        toString = $"{name} ({commandId})";
    }

    public ImageUri Icon { get; }
    public string Name { get; }
    public string CommandId { get; }
    private string toString;
        
    public override string ToString()
    {
        return toString;
    }
}