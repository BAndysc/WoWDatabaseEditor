using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Services.Mcp;
using WDE.Common.Solution;
using WDE.Common.Managers;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.ViewModels;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.Services.Mcp;

internal static class TableResolver
{
    public static DatabaseTableDefinitionJson Resolve(ITableDefinitionProvider definitionProvider, string table)
    {
        if (DatabaseTable.TryParse(table, out var parsed))
        {
            var definition = definitionProvider.GetDefinitionByTableName(parsed);
            if (definition != null)
                return definition;
        }

        // bare table name may live in the hotfix database or be a foreign (sub)table
        var hotfix = definitionProvider.GetDefinitionByTableName(DatabaseTable.HotfixTable(table));
        if (hotfix != null)
            return hotfix;

        var foreign = definitionProvider.GetDefinitionByForeignTableName(DatabaseTable.Parse(table))
                      ?? definitionProvider.GetDefinitionByForeignTableName(DatabaseTable.HotfixTable(table));
        if (foreign != null)
            return foreign;

        throw new McpToolException($"No editor definition for table '{table}' (see table_list)");
    }
}

public sealed class TableListInput
{
    [Description("Optional case-insensitive substring filter over table names and friendly names")]
    public string? Filter { get; init; }
}

public sealed class TableInfo
{
    public required string Table { get; init; }
    public required string FriendlyName { get; init; }
    public required string Database { get; init; }
    public required string RecordMode { get; init; }
    public string? Group { get; init; }
    public string? Description { get; init; }
    public required List<string> PrimaryKey { get; init; }
}

[AutoRegister]
[SingleInstance]
public class TableListTool : McpTool<TableListInput, List<TableInfo>>
{
    private readonly ITableDefinitionProvider definitionProvider;

    public TableListTool(ITableDefinitionProvider definitionProvider)
    {
        this.definitionProvider = definitionProvider;
    }

    public override string Name => "table_list";
    public override string Description => "Lists database tables known to the editor with their friendly names and editing mode (SingleRow = flat rows, MultiRecord = grouped by key, Template = one entity per key). These are the tables the generic table editor (and the table_* tools) can work with.";

    protected override Task<List<TableInfo>> Execute(TableListInput input, CancellationToken token)
    {
        var result = definitionProvider.Definitions
            .Where(d => string.IsNullOrEmpty(input.Filter) ||
                        d.TableName.Contains(input.Filter, StringComparison.OrdinalIgnoreCase) ||
                        d.Name.Contains(input.Filter, StringComparison.OrdinalIgnoreCase))
            .Select(d => new TableInfo
            {
                Table = d.TableName,
                FriendlyName = d.Name,
                Database = d.DataDatabaseType.ToString(),
                RecordMode = d.RecordMode.ToString(),
                Group = d.GroupName,
                Description = string.IsNullOrEmpty(d.Description) ? null : d.Description,
                PrimaryKey = d.PrimaryKey?.Select(k => k.ToString()).ToList() ?? new List<string>()
            })
            .OrderBy(t => t.Table)
            .ToList();
        return Task.FromResult(result);
    }
}

public sealed class TableDescribeInput
{
    [Description("Table name, i.e. 'creature_template' (optionally 'hotfix.some_table')")]
    public required string Table { get; init; }
}

public sealed class ColumnDescription
{
    [Description("Full database column name; foreign-table columns are prefixed with 'table.'")]
    public required string Column { get; init; }
    public required string FriendlyName { get; init; }
    public string? Help { get; init; }
    [Description("Value type: long/uint/int/float/string or a parameter key usable with parameter_search")]
    public required string ValueType { get; init; }
    public bool CanBeNull { get; init; }
    public bool ReadOnly { get; init; }
    public bool AutoIncrement { get; init; }
    public object? Default { get; init; }
}

public sealed class TableDescription
{
    public required TableInfo Table { get; init; }
    public required List<ColumnDescription> Columns { get; init; }
}

[AutoRegister]
[SingleInstance]
public class TableDescribeTool : McpTool<TableDescribeInput, TableDescription>
{
    private readonly ITableDefinitionProvider definitionProvider;

    public TableDescribeTool(ITableDefinitionProvider definitionProvider)
    {
        this.definitionProvider = definitionProvider;
    }

    public override string Name => "table_describe";
    public override string Description => "Describes a table the way the editor sees it: friendly column names, help texts and parameter value types (i.e. a column holding spell ids reports SpellParameter, searchable via parameter_search).";

    protected override Task<TableDescription> Execute(TableDescribeInput input, CancellationToken token)
    {
        var definition = TableResolver.Resolve(definitionProvider, input.Table);
        var columns = definition.TableColumns.Values
            .Where(c => c.IsActualDatabaseColumn)
            .Select(c => new ColumnDescription
            {
                Column = c.DbColumnFullName.ToString(),
                FriendlyName = c.Name,
                Help = c.Help,
                ValueType = c.ValueType,
                CanBeNull = c.CanBeNull,
                ReadOnly = c.IsReadOnly,
                AutoIncrement = c.AutoIncrement,
                Default = c.Default is null or bool or string ? c.Default : c.Default.ToString()
            })
            .ToList();

        return Task.FromResult(new TableDescription
        {
            Table = new TableInfo
            {
                Table = definition.TableName,
                FriendlyName = definition.Name,
                Database = definition.DataDatabaseType.ToString(),
                RecordMode = definition.RecordMode.ToString(),
                Group = definition.GroupName,
                Description = string.IsNullOrEmpty(definition.Description) ? null : definition.Description,
                PrimaryKey = definition.PrimaryKey?.Select(k => k.ToString()).ToList() ?? new List<string>()
            },
            Columns = columns
        });
    }
}

public sealed class TableSelectInput
{
    [Description("Table name, i.e. 'creature_template'")]
    public required string Table { get; init; }

    [Description("Optional raw SQL WHERE condition, i.e. 'entry BETWEEN 100 AND 200'")]
    public string? Where { get; init; }

    [Description("Primary key value(s) of the rows to load (alternative to where)")]
    public List<long>? Key { get; init; }

    [Description("Maximum number of rows (default 50)")]
    public int Limit { get; init; } = 50;

    [Description("Row offset for paging")]
    public long? Offset { get; init; }
}

public sealed class TableSelectOutput
{
    public required int RowCount { get; init; }
    [Description("Rows as column -> value maps")]
    public required List<Dictionary<string, object?>> Rows { get; init; }
    [Description("Per row: parameter columns resolved to readable names (only when known)")]
    public required List<Dictionary<string, string>> ReadableValues { get; init; }
}

[AutoRegister]
[SingleInstance]
public class TableSelectTool : McpTool<TableSelectInput, TableSelectOutput>
{
    private readonly ITableDefinitionProvider definitionProvider;
    private readonly IDatabaseTableDataProvider tableDataProvider;
    private readonly IParameterFactory parameterFactory;

    public TableSelectTool(ITableDefinitionProvider definitionProvider,
        IDatabaseTableDataProvider tableDataProvider,
        IParameterFactory parameterFactory)
    {
        this.definitionProvider = definitionProvider;
        this.tableDataProvider = tableDataProvider;
        this.parameterFactory = parameterFactory;
    }

    public override string Name => "table_select";
    public override string Description => "Loads rows of a table through the editor's data model: returns raw values plus readable names for parameter columns (spells, factions, flags...). Use table_describe first to see the columns.";

    protected override async Task<TableSelectOutput> Execute(TableSelectInput input, CancellationToken token)
    {
        var definition = TableResolver.Resolve(definitionProvider, input.Table);
        var keys = input.Key is { Count: > 0 } ? new[] { new DatabaseKey(input.Key) } : null;
        var data = await tableDataProvider.Load(definition.Id, input.Where, input.Offset, Math.Max(1, input.Limit), keys);
        if (data == null)
            throw new McpToolException("Could not load table data (is the database connected?)");

        var rows = new List<Dictionary<string, object?>>();
        var readable = new List<Dictionary<string, string>>();
        foreach (var entity in data.Entities)
        {
            token.ThrowIfCancellationRequested();
            var row = new Dictionary<string, object?>();
            var names = new Dictionary<string, string>();
            foreach (var field in entity.Fields)
            {
                var columnName = field.FieldName.ToString();
                var value = field.Object;
                row[columnName] = value is null or bool or string || value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal
                    ? value
                    : value.ToString();

                if (value != null &&
                    definition.TableColumns.TryGetValue(field.FieldName, out var columnDefinition) &&
                    columnDefinition.ValueType.EndsWith("Parameter", StringComparison.Ordinal) &&
                    !columnDefinition.IsTypeString &&
                    parameterFactory.IsRegisteredLong(columnDefinition.ValueType))
                {
                    try
                    {
                        var longValue = Convert.ToInt64(value);
                        var parameter = parameterFactory.Factory(columnDefinition.ValueType);
                        var asString = parameter.ToString(longValue);
                        if (asString != longValue.ToString())
                            names[columnName] = asString;
                    }
                    catch (Exception)
                    {
                        // unresolvable value - raw value is still in the row
                    }
                }
            }
            rows.Add(row);
            readable.Add(names);
        }

        return new TableSelectOutput
        {
            RowCount = rows.Count,
            Rows = rows,
            ReadableValues = readable
        };
    }
}

public enum TableOperation
{
    Insert,
    Update,
    Delete
}

public sealed class TableChangeInput
{
    [Description("Table name, i.e. 'creature_template'")]
    public required string Table { get; init; }

    [Description("What to do with the row(s)")]
    public required TableOperation Operation { get; init; }

    [Description("Primary key value(s) of the affected row(s); optional for insert when the key columns are part of fields")]
    public List<long>? Key { get; init; }

    [Description("For update: optional raw SQL WHERE selecting the rows to update instead of key (useful for MultiRecord tables)")]
    public string? Where { get; init; }

    [Description("Column -> value map (full column names as in table_describe)")]
    public Dictionary<string, JsonElement>? Fields { get; init; }

    [Description("When true, the generated SQL is also executed against the database. Default: false = only return the SQL for review")]
    public bool Execute { get; init; }
}

public sealed class TableChangeOutput
{
    public required string Sql { get; init; }
    public required bool Executed { get; init; }
    [Description("True when the change was applied to an open table editor document instead (visible, undoable; save it with document_save)")]
    public required bool AppliedToOpenDocument { get; init; }
    public string? Warning { get; init; }
}

[AutoRegister]
[SingleInstance]
public class TableChangeTool : McpTool<TableChangeInput, TableChangeOutput>
{
    private readonly ITableDefinitionProvider definitionProvider;
    private readonly IDatabaseTableDataProvider tableDataProvider;
    private readonly IDatabaseTableModelGenerator modelGenerator;
    private readonly IQueryGenerator queryGenerator;
    private readonly IDatabaseQueryExecutor queryExecutor;
    private readonly IDocumentManager documentManager;

    public TableChangeTool(ITableDefinitionProvider definitionProvider,
        IDatabaseTableDataProvider tableDataProvider,
        IDatabaseTableModelGenerator modelGenerator,
        IQueryGenerator queryGenerator,
        IDatabaseQueryExecutor queryExecutor,
        IDocumentManager documentManager)
    {
        this.definitionProvider = definitionProvider;
        this.tableDataProvider = tableDataProvider;
        this.modelGenerator = modelGenerator;
        this.queryGenerator = queryGenerator;
        this.queryExecutor = queryExecutor;
        this.documentManager = documentManager;
    }

    public override string Name => "table_change";
    public override string Description => "Inserts, updates or deletes rows of a table through the editor's model and query generator (same SQL as the editor's Save). Updates by key are applied to an OPEN table editor document when one shows the row (visible + undoable; user saves via document_save). Otherwise, by default only returns the SQL for review; pass execute=true to apply it.";
    public override bool Mutating => true;

    protected override async Task<TableChangeOutput> Execute(TableChangeInput input, CancellationToken token)
    {
        var definition = TableResolver.Resolve(definitionProvider, input.Table);

        var openDocument = FindOpenDocument(definition);
        if (input.Operation == TableOperation.Update && openDocument != null &&
            input.Key is { Count: > 0 } && input.Fields is { Count: > 0 } && input.Where == null)
        {
            var applied = TryApplyToOpenDocument(definition, openDocument, input);
            if (applied != null)
                return applied;
        }

        IQuery query = input.Operation switch
        {
            TableOperation.Insert => await GenerateInsert(definition, input),
            TableOperation.Update => await GenerateUpdate(definition, input),
            TableOperation.Delete => GenerateDelete(definition, input),
            _ => throw new McpToolException($"Unknown operation {input.Operation}")
        };

        if (input.Execute)
            await queryExecutor.ExecuteSql(definition, query);

        return new TableChangeOutput
        {
            Sql = query.QueryString,
            Executed = input.Execute,
            AppliedToOpenDocument = false,
            Warning = openDocument != null
                ? "A document for this table is open in the editor; it will not reflect this change until reloaded."
                : null
        };
    }

    private ViewModelBase? FindOpenDocument(DatabaseTableDefinitionJson definition)
    {
        foreach (var document in documentManager.OpenedDocuments)
        {
            if (document is ViewModelBase vm &&
                vm.SolutionItem is DatabaseTableSolutionItem item &&
                item.TableName == definition.Id)
                return vm;
        }
        return null;
    }

    // Applies field updates to the row(s) shown in the open editor document, like a user edit:
    // visible immediately, undoable, saved via the document's Save. Returns null when the row
    // isn't loaded in that document (caller falls back to the headless SQL path).
    private TableChangeOutput? TryApplyToOpenDocument(DatabaseTableDefinitionJson definition, ViewModelBase vm, TableChangeInput input)
    {
        var key = new DatabaseKey(input.Key!);
        var matched = vm.Entities.Where(e => e.Key == key).ToList();
        if (matched.Count == 0)
            return null;

        if (input.Execute)
            throw new McpToolException("This table's document is open in the editor and shows the row - nothing was changed. Call again with execute=false to edit the open document, then save it with document_save.");

        foreach (var entity in matched)
            ApplyFields(definition, entity, input.Fields!);

        var sql = queryGenerator.GenerateQuery(new[] { key }, null, new DatabaseTableData(definition, matched));
        return new TableChangeOutput
        {
            Sql = sql.QueryString,
            Executed = false,
            AppliedToOpenDocument = true,
            Warning = null
        };
    }

    private async Task<IQuery> GenerateInsert(DatabaseTableDefinitionJson definition, TableChangeInput input)
    {
        if (input.Fields == null || input.Fields.Count == 0)
            throw new McpToolException("Insert requires fields");

        var key = ResolveKey(definition, input, input.Fields);
        var entity = modelGenerator.CreateEmptyEntity(definition, key, false);
        ApplyFields(definition, entity, input.Fields);
        await Task.CompletedTask;
        return queryGenerator.GenerateInsertQuery(new[] { key }, new DatabaseTableData(definition, new List<DatabaseEntity> { entity }));
    }

    private async Task<IQuery> GenerateUpdate(DatabaseTableDefinitionJson definition, TableChangeInput input)
    {
        if (input.Fields == null || input.Fields.Count == 0)
            throw new McpToolException("Update requires fields");

        DatabaseKey[]? keys = null;
        if (input.Where == null)
        {
            if (input.Key is not { Count: > 0 })
                throw new McpToolException("Update requires key or where");
            keys = new[] { new DatabaseKey(input.Key) };
        }

        var data = await tableDataProvider.Load(definition.Id, input.Where, null, null, keys);
        if (data == null || data.Entities.Count == 0)
            throw new McpToolException("No rows matched the given key/where");

        foreach (var entity in data.Entities)
            ApplyFields(definition, entity, input.Fields);

        var affectedKeys = data.Entities.Select(e => e.GenerateKey(definition)).Distinct().ToList();
        return queryGenerator.GenerateQuery(affectedKeys, null, data);
    }

    private IQuery GenerateDelete(DatabaseTableDefinitionJson definition, TableChangeInput input)
    {
        if (input.Key is not { Count: > 0 })
            throw new McpToolException("Delete requires key");
        return queryGenerator.GenerateDeleteQuery(definition, new DatabaseKey(input.Key));
    }

    private DatabaseKey ResolveKey(DatabaseTableDefinitionJson definition, TableChangeInput input, Dictionary<string, JsonElement> fields)
    {
        if (input.Key is { Count: > 0 })
            return new DatabaseKey(input.Key);

        var primaryKey = definition.PrimaryKey;
        if (primaryKey == null || primaryKey.Count == 0)
            throw new McpToolException("Table has no primary key definition; pass key explicitly");

        var keyValues = new List<long>();
        foreach (var keyColumn in primaryKey)
        {
            var (_, element) = fields.FirstOrDefault(f => ColumnFullName.Parse(f.Key) == keyColumn);
            if (element.ValueKind is JsonValueKind.Number)
                keyValues.Add(element.GetInt64());
            else if (element.ValueKind is JsonValueKind.String && long.TryParse(element.GetString(), out var parsed))
                keyValues.Add(parsed);
            else
                throw new McpToolException($"Primary key column '{keyColumn}' not found in fields; pass key explicitly");
        }
        return new DatabaseKey(keyValues);
    }

    private void ApplyFields(DatabaseTableDefinitionJson definition, DatabaseEntity entity, Dictionary<string, JsonElement> fields)
    {
        foreach (var (columnName, value) in fields)
        {
            var fullName = ColumnFullName.Parse(columnName);
            if (!definition.TableColumns.TryGetValue(fullName, out var columnDefinition))
                throw new McpToolException($"Unknown column '{columnName}' (see table_describe)");
            if (entity.GetCell(fullName) == null)
                throw new McpToolException($"Column '{columnName}' is not editable on this row");

            try
            {
                if (columnDefinition.IsTypeString)
                    entity.SetTypedCellOrThrow(fullName, value.ValueKind == JsonValueKind.Null ? null : value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText());
                else if (columnDefinition.IsTypeFloat)
                    entity.SetTypedCellOrThrow(fullName, value.ValueKind == JsonValueKind.String ? float.Parse(value.GetString()!) : value.GetSingle());
                else
                    entity.SetTypedCellOrThrow(fullName, value.ValueKind == JsonValueKind.String ? long.Parse(value.GetString()!) : value.GetInt64());
            }
            catch (Exception e) when (e is not McpToolException)
            {
                throw new McpToolException($"Cannot set column '{columnName}' (type {columnDefinition.ValueType}): {e.Message}");
            }
        }
    }
}

public sealed class TableOpenEditorInput
{
    [Description("Table name, i.e. 'creature_template'")]
    public required string Table { get; init; }

    [Description("Optional primary key value(s) of the record to open (i.e. a creature entry)")]
    public List<long>? Key { get; init; }
}

[AutoRegister]
[SingleInstance]
public class TableOpenEditorTool : McpTool<TableOpenEditorInput, string>
{
    private readonly ITableDefinitionProvider definitionProvider;
    private readonly ITableOpenService tableOpenService;
    private readonly IEventAggregator eventAggregator;
    private readonly ISolutionItemNameRegistry nameRegistry;

    public TableOpenEditorTool(ITableDefinitionProvider definitionProvider,
        ITableOpenService tableOpenService,
        IEventAggregator eventAggregator,
        ISolutionItemNameRegistry nameRegistry)
    {
        this.definitionProvider = definitionProvider;
        this.tableOpenService = tableOpenService;
        this.eventAggregator = eventAggregator;
        this.nameRegistry = nameRegistry;
    }

    public override string Name => "table_open_editor";
    public override string Description => "Opens the generic table editor document for a table (optionally focused on a given record) in the editor UI, as if the user opened it themselves.";
    public override bool Mutating => true;

    protected override async Task<string> Execute(TableOpenEditorInput input, CancellationToken token)
    {
        var definition = TableResolver.Resolve(definitionProvider, input.Table);
        ISolutionItem? item;
        if (input.Key is { Count: > 0 })
            item = await tableOpenService.Create(definition, new DatabaseKey(input.Key));
        else
            item = await tableOpenService.TryCreate(definition);

        if (item == null)
            throw new McpToolException("Could not create an editor for this table (cancelled or unsupported)");

        eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
        return $"Opened {nameRegistry.GetName(item)}";
    }
}
