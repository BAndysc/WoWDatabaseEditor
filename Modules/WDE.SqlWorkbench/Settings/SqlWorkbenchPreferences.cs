using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Settings;

[SingleInstance]
[AutoRegister]
internal class SqlWorkbenchPreferences : ISqlWorkbenchPreferences
{
    private readonly IUserSettings userSettings;
    private ConnectionsSettings settings;
    
    public SqlWorkbenchPreferences(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        settings = userSettings.Get(new ConnectionsSettings())!;
    }
    
    public IReadOnlyList<DatabaseConnectionData> Connections
    {
        get => settings.Connections;
        set
        {
            settings.Connections = value.Select(x =>
            {
                if (x.CredentialsSource != CredentialsSource.Custom)
                    return x.WithCredentials(new DatabaseCredentials("", "", "", 0, x.Credentials.SchemaName));
                return x;
            }).ToList();
        }
    }
    
    public Guid? DefaultConnection
    {
        get => settings.DefaultConnection;
        set => settings.DefaultConnection = value ?? Guid.Empty;
    }

    public bool UseCodeCompletion
    {
        get => settings.UseCodeCompletion;
        set => settings.UseCodeCompletion = value;
    }

    public string? CustomSqlsPath
    {
        get => settings.CustomSqlsPath;
        set => settings.CustomSqlsPath = value;
    }

    public string? CustomMySqlDumpPath
    {
        get => settings.CustomMySqlDumpPath;
        set => settings.CustomMySqlDumpPath = value;
    }
    
    public string? CustomMariaDumpPath
    {
        get => settings.CustomMariaDumpPath;
        set => settings.CustomMariaDumpPath = value;
    }

    public string? CustomMySqlImportPath
    {
        get => settings.CustomMySqlImportPath;
        set => settings.CustomMySqlImportPath = value;
    }

    public string? CustomMariaImportPath
    {
        get => settings.CustomMySqlImportPath;
        set => settings.CustomMySqlImportPath = value;
    }

    public bool AskBeforeApplyingChanges 
    {
        get => settings.AskBeforeApplyingChanges;
        set => settings.AskBeforeApplyingChanges = value;
    }

    public bool CloseNonModifiedTabsOnExecute
    {
        get => settings.CloseNonModifiedTabsOnExecute;
        set => settings.CloseNonModifiedTabsOnExecute = value;
    }

    public bool EachDatabaseHasSeparateConnection
    {
        get => settings.EachDatabaseHasSeparateConnection;
        set => settings.EachDatabaseHasSeparateConnection = value;
    }

    public void Save()
    {
        userSettings.Update(settings);
    }

    private class ConnectionsSettings : ISettings
    {
        public Guid DefaultConnection { get; set; }
        
        public List<DatabaseConnectionData> Connections { get; set; } = new();
        
        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool UseCodeCompletion { get; set; } = true;
        
        public string? CustomSqlsPath { get; set; }
        
        public string? CustomMySqlDumpPath { get; set; }
        
        public string? CustomMariaDumpPath { get; set; }

        public string? CustomMySqlImportPath { get; set; }

        public string? CustomMariaImportPath { get; set; }
        
        [DefaultValue(true)]
        public bool EachDatabaseHasSeparateConnection { get; set; } = true;
        
        [DefaultValue(true)]
        public bool AskBeforeApplyingChanges { get; set; } = true;
        
        [DefaultValue(true)]
        public bool CloseNonModifiedTabsOnExecute { get; set; } = true;
    }
}