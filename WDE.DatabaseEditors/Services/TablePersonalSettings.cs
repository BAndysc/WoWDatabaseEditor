using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.Services;

[AutoRegister]
[SingleInstance]
public class TablePersonalSettings : ITablePersonalSettings
{
    private readonly IUserSettings userSettings;
    private Dictionary<DatabaseTable, TablePersonalData> data = new();
    private ValuePublisher<Unit> saveRequests = new();

    public TablePersonalSettings(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        this.data = userSettings.Get<Data>(new())!.data;
        saveRequests.Throttle(TimeSpan.FromSeconds(1)).SubscribeAction(_ =>
        {
            DoSave();
        });
    }

    private void DoSave()
    {
        userSettings.Update(new Data(){data = data});;
    }

    public double GetColumnWidth(DatabaseTable table, string column, double defaultWidth)
    {
        if (!data.TryGetValue(table, out var tableData))
            return defaultWidth;

        if (tableData.customWidths.TryGetValue(column, out var columnData))
            return columnData;

        return defaultWidth;
    }

    public bool IsColumnVisible(DatabaseTable table, string column, bool defaultIsVisible = true)
    {
        if (!data.TryGetValue(table, out var tableData))
            return defaultIsVisible;
        
        return !tableData.hiddenColumns.Contains(column) && defaultIsVisible;
    }

    public void UpdateWidth(DatabaseTable table, string column, double defaultWidth, double width)
    {
        bool isDefault = Math.Abs(defaultWidth - width) < 5;
        if (!data.TryGetValue(table, out var tableData))
        {
            // no data & isDefault width == default, so let's not touch it 
            if (isDefault)
                return;
            tableData = new TablePersonalData();
            data.Add(table, tableData);
        }

        tableData.customWidths[column] = width;
        saveRequests.Publish(default);
    }

    public void UpdateVisibility(DatabaseTable table, string column, bool isVisible)
    {
        if (!data.TryGetValue(table, out var tableData))
        {
            // no data & make column visible == default, so let's not touch it 
            if (isVisible)
                return;
            tableData = new TablePersonalData();
            data.Add(table, tableData);
        }

        if (isVisible)
            tableData!.hiddenColumns.Remove(column);
        else if (!tableData!.hiddenColumns.Contains(column))
            tableData!.hiddenColumns.Add(column);
        saveRequests.Publish(default);
    }

    private class TablePersonalData
    {
        public Dictionary<string, double> customWidths = new();
        public List<string> hiddenColumns = new();
        
        public TablePersonalData(Dictionary<string, double>? customWidths = null, List<string>? hiddenColumns = null)
        {
            this.customWidths = customWidths ?? new();
            this.hiddenColumns = hiddenColumns ?? new();
        }
    }

    private class Data : ISettings
    {
        public Dictionary<DatabaseTable, TablePersonalData> data = new();
        
        public Data(Dictionary<DatabaseTable, TablePersonalData>? data = null)
        {
            this.data = data ?? new();
        }
    }
}