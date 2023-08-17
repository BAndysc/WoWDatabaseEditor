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
    public static IEnumerable<IconViewModel> Icons { get; }
    public static IEnumerable<object> IconsSource { get; } // todo: CompletionComboBox Items should be IEnumerable, not IEnumerable<object>
                                                           // otherwise we need this extra redundant property here

    static DefinitionEditorStatic()
    {
        var icons = new List<IconViewModel>();

        if (!OperatingSystem.IsBrowser())
        {
            var files = Directory.GetFiles("Icons/", "*.png");
            foreach (var file in files)
            {
                if (file.Contains("@2x"))
                    continue;
                
                if (file.Contains("_big"))
                    continue;

                if (file.Contains("_dark"))
                    continue;
                
                string relativePath;
                if (file.Contains("Icons/"))
                    relativePath = file.Substring(file.IndexOf("Icons/", StringComparison.Ordinal));
                else if (file.Contains("Icons\\"))
                   relativePath = file.Substring(file.IndexOf("Icons\\", StringComparison.Ordinal));
                else
                    continue;
                
                icons.Add(new IconViewModel(new ImageUri(relativePath)));
            }
        }
        
        Icons = icons;
        IconsSource = icons;
    }
}