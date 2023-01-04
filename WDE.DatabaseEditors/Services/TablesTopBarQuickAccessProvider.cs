using System;
using System.Collections.Generic;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.CoreVersion;
using WDE.Common.QuickAccess;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

[AutoRegister]
public class TablesTopBarQuickAccessProvider : ITopBarQuickAccessProvider
{
    public TablesTopBarQuickAccessProvider(ITableDefinitionProvider definitionProvider,
        ICurrentCoreVersion currentCoreVersion,
        IStandaloneTableEditService standaloneTableEditService)
    {
        List<ITopBarQuickAccessItem> items = new(); 
        foreach (var (table, enabled) in currentCoreVersion.Current.TopBarQuickTableEditors)
        {
            var definition = definitionProvider.GetDefinition(table);
            if (definition == null)
            {
                continue;
                throw new Exception("Table " + table + " not found while constructing top bar quick access items");
            }

            var icon = new ImageUri(definition.IconPath ?? "Icons/document.png");
             
            if (enabled)
            {
                items.Add(new TableTopBarQuickAccessItem(definition.Name, icon, new DelegateCommand(() =>
                {
                    standaloneTableEditService.OpenEditor(definition.Id);
                })));
            }
            else
            {
                items.Add(new TableTopBarQuickAccessItem(definition.Name, icon, AlwaysDisabledCommand.Command));
            }
        }

        Items = items;
    }
    
    public IEnumerable<ITopBarQuickAccessItem> Items { get; }
    
    public int Order => 0;

    private class TableTopBarQuickAccessItem : ITopBarQuickAccessItem
    {
        public TableTopBarQuickAccessItem(string name, ImageUri icon, ICommand command)
        {
            Name = name;
            Icon = icon;
            Command = command;
        }
        
        public ICommand Command { get; }
        public string Name { get; }
        public ImageUri Icon { get; }
    }
}