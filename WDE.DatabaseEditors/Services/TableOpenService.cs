using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Solution;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

[AutoRegister]
[SingleInstance]
public class TableOpenService : ITableOpenService
{
    private readonly IParameterFactory parameterFactory;
    private readonly IItemFromListProvider itemFromListProvider;
    private readonly IDatabaseTableDataProvider tableDataProvider;
    private readonly ITableDefinitionProvider definitionProvider;
    private readonly IMessageBoxService messageBoxService;

    public TableOpenService(
        IParameterFactory parameterFactory,
        IItemFromListProvider itemFromListProvider,
        IDatabaseTableDataProvider tableDataProvider,
        ITableDefinitionProvider definitionProvider,
        IMessageBoxService messageBoxService)
    {
        this.parameterFactory = parameterFactory;
        this.itemFromListProvider = itemFromListProvider;
        this.tableDataProvider = tableDataProvider;
        this.definitionProvider = definitionProvider;
        this.messageBoxService = messageBoxService;
    }
    
    public async Task<ISolutionItem?> TryCreate(DatabaseTableDefinitionJson definition)
    {
        if (definition.RecordMode == RecordMode.SingleRow)
            return await Create(definition, default);
        
        Debug.Assert(definition.GroupByKeys.Count == 1);
        
        var parameter = parameterFactory.Factory(definition.Picker);
        var key = await itemFromListProvider.GetItemFromList(parameter.HasItems ? parameter.Items! : new Dictionary<long, SelectOption>(), false);
        if (key.HasValue)
        {
            return await Create(definition, new DatabaseKey(key.Value));
        }
        return null;
    }
    
    public async Task<ISolutionItem?> Create(DatabaseTableDefinitionJson definition, DatabaseKey key)
    {
        if (definition.RecordMode == RecordMode.MultiRecord)
            return new DatabaseTableSolutionItem(key, false, false, definition.Id, definition.IgnoreEquality);
        if (definition.RecordMode == RecordMode.SingleRow)
            return new DatabaseTableSolutionItem(definition.Id, definition.IgnoreEquality);
        else
        {
            var data = await tableDataProvider.Load(definition.Id, null, null,null, new []{key});
                
            if (data == null)
                return null;
            if (data.Entities.Count == 0)
                return null; 
                    
            // actually, why not, let's allow creating new entities via this editor
            // if (!data.Entities[0].ExistInDatabase)
            // {
            //     if (!await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            //             .SetTitle("Entity doesn't exist in database")
            //             .SetMainInstruction($"Entity {data.Entities[0].Key} doesn't exist in the database")
            //             .SetContent(
            //                 "WoW Database Editor will be generating DELETE/INSERT query instead of UPDATE. Do you want to continue?")
            //             .WithYesButton(true)
            //             .WithNoButton(false).Build()))
            //         return null;
            // }
            return new DatabaseTableSolutionItem(data.Entities[0].Key, data.Entities[0].ExistInDatabase, data.Entities[0].ConditionsModified, definition.Id, definition.IgnoreEquality);
        }
    }

}