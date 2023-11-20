using System.Collections.Generic;
using System.Linq;
using WDE.Common.Utils;
using WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;
using WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor.Services;

[AutoRegister]
[SingleInstance]
public class DefinitionExporterService : IDefinitionExporterService
{
    public DatabaseTableDefinitionJson Export(DefinitionViewModel vm)
    {
        var groups = vm.Groups.Select(Export).ToList();
        List<string>? primaryKey;
        
        if (vm.HasCustomPrimaryKeyOrder)
            primaryKey = vm.CustomPrimaryKey.Select(c => c.ColumnName).ToList();
        else
        {
            primaryKey = new List<string>();
            foreach (var candidate in vm.Groups.SelectMany(g => g.Columns)
                         .Where(c => c.IsPrimaryKey && c.DatabaseColumnName != null && !c.DatabaseColumnName.IsForeignTable)
                         .Select(c => c.DatabaseColumnName!.ColumnName))
            {
                if (!primaryKey.Contains(candidate))
                    primaryKey.Add(candidate);
            }    
        }
        
        if (primaryKey.Count == 0)
            primaryKey = null;

        var foreignKeys = vm.ForeignTables.Select(f => new DatabaseForeignTableJson()
        {
            TableName = f.TableName,
            AutofillBuildColumn = f.AutofillBuildColumn.NullIfEmpty(),
            ForeignKeys = f.ForeignKeys.Select(k => k.ColumnName).ToArray()
        }).ToList();
        if (foreignKeys.Count == 0)
            foreignKeys = null;
        
        return new DatabaseTableDefinitionJson()
        {
            Compatibility = vm.Compatibility.Where(c => c.IsChecked).Select(core => core.Tag).ToList(),
            Name = vm.Name,
            SingleSolutionName = vm.SingleSolutionName,
            MultiSolutionName = vm.MultiSolutionName,
            Description = vm.Description,
            TableName = vm.TableName,
            TablePrimaryKeyColumnName = vm.TablePrimaryKeyColumnName,
            RecordMode = vm.RecordMode,
            DataDatabaseType = vm.DataDatabaseType,
            IsOnlyConditionsTable = vm.IsOnlyConditionsTable,
            SkipQuickLoad = vm.SkipQuickLoad,
            GroupName = vm.GroupName,
            IconPath = vm.IconPath?.Path,
            ReloadCommand = vm.ReloadCommand,
            SortBy = vm.SortBy.Count == 0 ? null : vm.SortBy.Select(s => s.ColumnName).ToArray(),
            Picker = vm.Picker?.ParameterName ?? "",
            TableNameSource = vm.TableNameSource.NullIfEmpty(),
            PrimaryKey = primaryKey!,
            Commands = vm.Commands.Count == 0 ? null : vm.Commands.Select(Export).ToArray(),
            GroupByKey = vm.GroupByKey.NullIfEmpty(),
            Condition = vm.HasCondition ? Export(vm.Condition!) : null,
            ForeignTable = foreignKeys,
            AutofillBuildColumn = vm.AutofillBuildColumn.NullIfEmpty(),
            Groups = groups,
            AutoKeyValue = vm.AutoKeyValue,
        };
    }

    private DatabaseCommandDefinitionJson Export(CommandViewModel vm)
    {
        var parameters = vm.Parameters.Count == 0
            ? null
            : vm.Parameters.Select(p => p.ColumnName).ToArray();
        return new DatabaseCommandDefinitionJson()
        {
            CommandId = vm.CommandId,
            KeyBinding = vm.KeyGesture?.ToString(),
            Usage = vm.Usage,
            Parameters = parameters
        };
    }
    
    private DatabaseConditionReferenceJson Export(ConditionReferenceViewModel vm)
    {
        return new DatabaseConditionReferenceJson()
        {
            SourceType = vm.SourceType,
            SourceEntryColumn = vm.SourceEntryColumn,
            SourceIdColumn = vm.SourceIdColumn,
            SetColumn = vm.SetColumn,
            SourceGroupColumn = vm.SourceGroupColumn == null ? null :
                new DatabaseConditionColumn(){IsAbs = vm.SourceGroupColumnAbs, Name = vm.SourceGroupColumn},
            Targets = vm.Targets.Select(t => new DatabaseConditionTargetJson()
            {
                Id = t.Id,
                Name = t.Name
            }).ToList()
        };
    }

    private DatabaseColumnsGroupJson Export(ColumnGroupViewModel vm)
    {
        List<DatabaseColumnJson> columns = vm.Columns.Select(Export).ToList();
        return new DatabaseColumnsGroupJson()
        {
            Name = vm.GroupName,
            Fields = columns,
            ShowIf = vm.HasShowIf ? new DatabaseColumnsGroupJson.ShowIfCondition(){ColumnName = vm.ShowIfColumnName, Value = vm.ShowIfColumnValue} : null
        };
    }
    
    private DatabaseColumnJson Export(ColumnViewModel vm)
    {
        if (vm.IsMetaColumnType)
        {
            return new DatabaseColumnJson()
            {
                Name = vm.Header,
                Help = vm.Help,
                ColumnIdForUi = vm.ColumnIdForUi,
                IsReadOnly = vm.IsReadOnly,
                Meta = ExportMeta(vm.MetaViewModel),
                PreferredWidth = vm.IntWidth == 100 ? null : vm.IntWidth
            };
        }

        if (vm.IsConditionColumnType)
        {
            return new DatabaseColumnJson()
            {
                Name = vm.Header,
                Help = vm.Help,
                DbColumnName = "conditions",
                ColumnIdForUi = vm.ColumnIdForUi,
                IsConditionColumn = true,
                PreferredWidth = vm.IntWidth == 100 ? null : vm.IntWidth
            };    
        }
        
        return new DatabaseColumnJson()
        {
            Name = vm.Header,
            Help = vm.Help,
            DbColumnName = vm.DatabaseColumnName?.ColumnName ?? "",
            ColumnIdForUi = vm.ColumnIdForUi,
            ForeignTable = vm.DatabaseColumnName?.IsForeignTable ?? false ? vm.DatabaseColumnName?.TableName : null,
            ValueType = vm.ValueType == null ? "" : (vm.ValueType.IsParameter ? vm.ValueType.ParameterName : vm.ValueType.ValueTypeName) ?? "",
            Default = ExportDefaultValue(vm.DefaultValue, vm.ValueType),
            AutoIncrement = vm.IsAutoIncrement,
            IsReadOnly = vm.IsReadOnly,
            CanBeNull = vm.CanBeNull,
            IsConditionColumn = vm.IsConditionColumnType,
            IsZeroBlank = vm.IsZeroBlank,
            AutogenerateComment = vm.AutogenerateComment,
            Meta = ExportMeta(vm.MetaViewModel),
            PreferredWidth = vm.IntWidth == 100 ? null : vm.IntWidth
        };
    }

    private object? ExportDefaultValue(string? stringDefault, ColumnValueTypeViewModel? valueType)
    {
        if (stringDefault == null || valueType == null)
            return null;
        
        if (valueType.IsNumeric && long.TryParse(stringDefault, out var longDefault))
            return longDefault;
        
        if (valueType.IsNumeric && double.TryParse(stringDefault, out var floatDefault))
            return floatDefault;
        
        return stringDefault;
    }

    private string? ExportMeta(BaseMetaColumnViewModel? meta)
    {
        if (meta == null)
            return null;
        return meta.Export();
    }
}