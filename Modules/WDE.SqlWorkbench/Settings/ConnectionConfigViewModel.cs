using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Media;
using AvaloniaStyles.Utils;
using PropertyChanged.SourceGenerator;
using WDE.Common.Types;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;
using HslColor = AvaloniaStyles.Utils.HslColor;

namespace WDE.SqlWorkbench.Settings;

internal partial class ConnectionConfigViewModel : ObservableBase
{
    private Guid id;
    public Guid Id => id;
    private HslColor hslColor = new HslColor(0, 1, 0.5f);
    [Notify] [AlsoNotify(nameof(IsCustomCredentials))] private CredentialsSource credentialsSource;
    [Notify] [AlsoNotify(nameof(DefaultConnectionName), nameof(ConnectionNameOrFallBack))] private string host = "";
    [Notify] private int port = 3306;
    [Notify] [AlsoNotify(nameof(DefaultConnectionName), nameof(ConnectionNameOrFallBack))] private string user = "";
    [Notify]  private string password = "";
    [Notify]  private string defaultDatabase = "";
    [Notify]  [AlsoNotify(nameof(ConnectionNameOrFallBack))] private string connectionName = "";
    [Notify]  private string? icon = "";
    [Notify]  private bool defaultExpandSchemas;
    [Notify] [AlsoNotify(nameof(Color))] private bool hasColor;
    [Notify]  private QueryExecutionSafety safeMode;
    public Color? Color => hasColor ? hslColor.ToRgba() : null;
    
    public static List<QueryExecutionSafety> SafeModes { get; } = Enum.GetValues(typeof(QueryExecutionSafety)).Cast<QueryExecutionSafety>().ToList();
    
    private bool colorTouched;
    public HslColor HslColor
    {
        get => hslColor;
        set
        {
            colorTouched = true;
            SetProperty(ref hslColor, value);
            RaisePropertyChanged(nameof(Color));
        }
    }

    private List<string>? visibleSchemas;
    public List<string>? VisibleSchemas
    {
        get => visibleSchemas;
        set
        {
            visibleSchemas = value;
            visibleSchemasTouched = true;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(VisibleSchemasAsText));
            RaisePropertyChanged(nameof(ShowOnlyVisibleSchemas));
        }
    }
    
    private bool visibleSchemasTouched;
    public bool ShowOnlyVisibleSchemas
    {
        get => visibleSchemas != null;
        set
        {
            if (value && VisibleSchemas == null)
                VisibleSchemas = new List<string>();
            else if (!value)
                VisibleSchemas = null;
        }
    }
    
    public string VisibleSchemasAsText => visibleSchemas == null ? "" : string.Join(",", visibleSchemas);
    
    public bool IsCustomCredentials => credentialsSource == CredentialsSource.Custom;
    
    private DatabaseConnectionData original;

    public string DefaultConnectionName => $"{User}@{Host}";
    
    public string ConnectionNameOrFallBack => string.IsNullOrEmpty(ConnectionName) ? DefaultConnectionName : ConnectionName;
    
    public bool IsModified => original.Credentials.Host != Host ||
                              original.Credentials.Port != Port ||
                              original.Credentials.User != User ||
                              original.Credentials.Passwd != Password ||
                              original.Credentials.SchemaName != DefaultDatabase ||
                              original.Icon?.Uri != Icon ||
                              original.ConnectionName != ConnectionName ||
                              original.CredentialsSource != CredentialsSource ||
                              original.DefaultExpandSchemas != DefaultExpandSchemas ||
                              original.Color.HasValue != hasColor || 
                              colorTouched ||
                              visibleSchemasTouched ||
                              SafeMode != original.SafeMode;

    public DatabaseConnectionData Original
    {
        get => original;
        set
        {
            original = value;
            colorTouched = false;
            visibleSchemasTouched =false;
            RaisePropertyChanged(nameof(IsModified));
        }
    }
    
    public ConnectionConfigViewModel(DatabaseConnectionData data)
    {
        id = data.Id;
        if (id == Guid.Empty)
            id = Guid.NewGuid();
        original = data;
        CredentialsSource = data.CredentialsSource;
        Host = data.Credentials.Host;
        Port = data.Credentials.Port;
        User = data.Credentials.User;
        Password = data.Credentials.Passwd;
        DefaultDatabase = data.Credentials.SchemaName;
        ConnectionName = data.ConnectionName;
        Icon = data.Icon?.Uri;
        DefaultExpandSchemas = data.DefaultExpandSchemas;
        HasColor = data.Color != null;
        VisibleSchemas = data.VisibleSchemas?.ToList();
        if (data.Color != null)
            HslColor = HslColor.FromRgba(data.Color.Value);
        colorTouched = false;
        visibleSchemasTouched = false;
        safeMode = data.SafeMode;
        
        this.PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IsModified))
            RaisePropertyChanged(nameof(IsModified));
    }

    public DatabaseConnectionData ToConnectionData()
    {
        return new DatabaseConnectionData(id, 
            credentialsSource,
            new DatabaseCredentials(User, Password, Host, Port, DefaultDatabase), 
            ConnectionName,  
            icon == null ? null : new ImageUri(icon),
            defaultExpandSchemas,
            hasColor ? hslColor.ToRgba() : null,
            visibleSchemas?.ToArray(),
            safeMode);
    }
}