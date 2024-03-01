using System;
using System.Collections.Generic;
using System.IO;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseDefinitionEditor.ViewModels;

public static class DefinitionEditorStatic
{
    public static IReadOnlyList<ColumnType> ColumnTypes { get; } = new List<ColumnType>() { ColumnType.Database, ColumnType.Meta, ColumnType.Condition };
    public static IReadOnlyList<RecordMode> RecordModes { get; } = new List<RecordMode>() { RecordMode.Template, RecordMode.MultiRecord, RecordMode.SingleRow };
    public static IReadOnlyList<DataDatabaseType> DataDatabaseTypes { get; } = new List<DataDatabaseType>()     { DataDatabaseType.World, DataDatabaseType.Hotfix };
    public static IReadOnlyList<OnlyConditionMode> OnlyConditionModes { get; } = new List<OnlyConditionMode>() { OnlyConditionMode.None, OnlyConditionMode.IgnoreTableCompletely, OnlyConditionMode.TableReadOnly};
    public static IReadOnlyList<GuidType?> GuidTypes { get; } = new List<GuidType?>() { null, GuidType.Creature, GuidType.GameObject };
}