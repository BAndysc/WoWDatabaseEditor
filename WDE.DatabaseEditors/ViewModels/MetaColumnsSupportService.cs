using System;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Utils;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.ViewModels;

[UniqueProvider]
public interface IMetaColumnsSupportService
{
    ICommand GenerateCommand(string metaColumn, DatabaseEntity entity, DatabaseKey realKey);
}

[AutoRegister]
[SingleInstance]
public class MetaColumnsSupportService : IMetaColumnsSupportService
{
    private readonly ITableEditorPickerService tableEditorPickerService;
    private readonly ITableDefinitionProvider definitionProvider;

    public MetaColumnsSupportService(ITableEditorPickerService tableEditorPickerService,
        ITableDefinitionProvider definitionProvider)
    {
        this.tableEditorPickerService = tableEditorPickerService;
        this.definitionProvider = definitionProvider;
    }
    
    public ICommand GenerateCommand(string metaColumn, DatabaseEntity entity, DatabaseKey realKey)
    {
        if (metaColumn.StartsWith("table:"))
        {
            var parts = metaColumn.Substring(6).Split(';');
            if (parts.Length < 2)
                throw new NotSupportedException("Invalid table meta column: " + metaColumn + ". Expected `table:<tableName>;<condition>(;<keys>)?`");
            var table = parts[0];
            var condition = parts[1];
            var keyParts = parts.Length == 3 ? parts[2].Split(',') : new string[]{};
            return new DelegateCommand(
                () =>
                {
                    var newCondition = entity.FillTemplate(condition);
                    tableEditorPickerService.ShowTable(table, newCondition, keyParts.Length == 0 ? null : new DatabaseKey(keyParts.Select(entity.GetTypedValueOrThrow<long>))).ListenErrors();
                });
        }
        if (metaColumn.StartsWith("tableByKey:"))
        {
            var table = metaColumn.Substring(11, metaColumn.IndexOf(";") - 11);
            var key = metaColumn.Substring(metaColumn.IndexOf(";") + 1);
            return new DelegateCommand(() =>
            {
                tableEditorPickerService.ShowTable(table, null, new DatabaseKey(entity.GetTypedValueOrThrow<long>(key))).ListenErrors();
            });
        }
        if (metaColumn.StartsWith("one2one:"))
        {
            var table = metaColumn.Substring(8);
            return new DelegateCommand(
                () =>
                {
                    tableEditorPickerService.ShowForeignKey1To1(table, entity.Key).ListenErrors();
                }, () => !entity.Phantom);
        }

        return new AlwaysDisabledCommand();
    }
}