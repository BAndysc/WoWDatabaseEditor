using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.DatabaseEditors.CustomCommands;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Utils;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.ViewModels;

[UniqueProvider]
public interface IMetaColumnsSupportService
{
    (ICommand, string) GenerateCommand(ViewModelBase? viewModel, DataDatabaseType database, string metaColumn, DatabaseEntity entity, DatabaseKey realKey);
}

[AutoRegister]
[SingleInstance]
public class MetaColumnsSupportService : IMetaColumnsSupportService
{
    private readonly ITableEditorPickerService tableEditorPickerService;
    private readonly ITableDefinitionProvider definitionProvider;
    private readonly IRemoteConnectorService remoteConnectorService;
    private readonly IDatabaseTableCommandService commandService;

    public MetaColumnsSupportService(ITableEditorPickerService tableEditorPickerService,
        ITableDefinitionProvider definitionProvider,
        IRemoteConnectorService remoteConnectorService,
        IDatabaseTableCommandService commandService)
    {
        this.tableEditorPickerService = tableEditorPickerService;
        this.definitionProvider = definitionProvider;
        this.remoteConnectorService = remoteConnectorService;
        this.commandService = commandService;
    }
    
    public (ICommand, string) GenerateCommand(ViewModelBase? viewModel, DataDatabaseType database, string metaColumn, DatabaseEntity entity, DatabaseKey realKey)
    {
        if (metaColumn.StartsWith("table:"))
        {
            var parts = metaColumn.Substring(6).Split(';');
            if (parts.Length < 2)
                throw new NotSupportedException("Invalid table meta column: " + metaColumn + ". Expected `table:<tableName>;<condition>(;<keys>)?`");
            var table = DatabaseTable.Parse(parts[0], database);
            var condition = parts[1];
            var keyParts = parts.Length == 3 ? parts[2].Split(',') : new string[]{};
            return (new DelegateCommand(
                () =>
                {
                    var newCondition = entity.FillTemplate(condition);
                    tableEditorPickerService.ShowTable(table, newCondition, keyParts.Length == 0 ? null : new DatabaseKey(keyParts.Select(key => entity.GetTypedValueOrThrow<long>(new ColumnFullName(null, key))))).ListenErrors();
                }), "Open");
        }
        if (metaColumn.StartsWith("tableByKey:"))
        {
            var table = DatabaseTable.Parse(metaColumn.Substring(11, metaColumn.IndexOf(";") - 11), database);
            var key = metaColumn.Substring(metaColumn.IndexOf(";") + 1);
            return (new DelegateCommand(() =>
            {
                tableEditorPickerService.ShowTable(table, null, new DatabaseKey(entity.GetTypedValueOrThrow<long>(new ColumnFullName(null, key)))).ListenErrors();
            }), "Open");
        }
        if (metaColumn.StartsWith("one2one:"))
        {
            var table = DatabaseTable.Parse(metaColumn.Substring(8), database);
            return (new DelegateCommand(
                () =>
                {
                    tableEditorPickerService.ShowForeignKey1To1(table, entity.Key).ListenErrors();
                }, () => !entity.Phantom), "Open");
        }
        if (metaColumn.StartsWith("one2one_dynamic_key:"))
        {
            var table = DatabaseTable.Parse(metaColumn.Substring(20), database);
            return (new DelegateCommand(
                () =>
                {
                    var definition = definitionProvider.GetDefinitionByTableName(table);
                    if (definition == null)
                        throw new UnsupportedTableException(table);
                    var generatedKey = entity.ForceGenerateKey(definition);
                    tableEditorPickerService.ShowForeignKey1To1(table, generatedKey).ListenErrors();
                }, () => !entity.Phantom), "Open");
        }
        if (metaColumn.StartsWith("invoke:"))
        {
            var command = metaColumn.Substring(7);
            return (new AsyncAutoCommand(
                () =>
                {
                    var result = entity.FillTemplate(command);
                    return remoteConnectorService.ExecuteCommand(new AnonymousRemoteCommand(result));
                }, () => !entity.Phantom), "Invoke");
        }
        if (metaColumn.StartsWith("command:"))
        {
            var commandName = metaColumn.Substring(8);
            string text = "⏩";
            if (commandName.Contains(":"))
            {
                var colon = commandName.IndexOf(":", StringComparison.Ordinal);
                text = commandName.Substring(colon + 1);
                commandName = commandName.Substring(0, colon);
            }
            var command = commandService.FindCommand(commandName);
            if (command == null || viewModel == null)
                return (new AlwaysDisabledCommand(), "(invalid)");

            var definition = new DatabaseCommandDefinitionJson() { CommandId = commandName };
            
            return (new AsyncAutoCommand(async () =>
            {
                await command.Process(definition, new DatabaseTableData(viewModel.TableDefinition, viewModel.Entities), entity, viewModel);
            }, () => command.CanExecute(definition, entity, viewModel)), text);
        }

        if (metaColumn.StartsWith("picker"))
        {
            if (viewModel == null)
                return  (new AlwaysDisabledCommand(), "⬅");
            return (new DelegateCommand(() =>
            {
                viewModel.TryPick(entity);
            }), "⬅");
        }

        return (new AlwaysDisabledCommand(), "(invalid)");
    }
}