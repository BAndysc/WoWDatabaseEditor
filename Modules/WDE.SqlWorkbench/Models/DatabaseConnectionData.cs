using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WDE.Common.Types;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.Models;

[JsonConverter(typeof(StringEnumConverter))]
internal enum CredentialsSource
{
    Custom,
    WorldDatabase,
    AuthDatabase,
    HotfixDatabase
}

internal readonly struct DatabaseConnectionData
{
    public readonly Guid Id;
    public readonly CredentialsSource CredentialsSource;
    public readonly DatabaseCredentials Credentials;
    public readonly string ConnectionName;
    public readonly ImageUri? Icon;
    public readonly bool DefaultExpandSchemas;
    public readonly Color? Color;
    public readonly string[]? VisibleSchemas;
    public readonly QueryExecutionSafety SafeMode;

    public bool IsTemporary => Id == Guid.Empty;
    
    public DatabaseConnectionData(Guid id,
        CredentialsSource credentialsSource,
        DatabaseCredentials credentials,
        string connectionName,
        ImageUri? icon,
        bool defaultExpandSchemas,
        Color? color, 
        string[]? visibleSchemas,
        QueryExecutionSafety safeMode)
    {
        Credentials = credentials;
        CredentialsSource = credentialsSource;
        ConnectionName = connectionName;
        Id = id;
        Icon = icon;
        DefaultExpandSchemas = defaultExpandSchemas;
        Color = color;
        VisibleSchemas = visibleSchemas?.ToArray();
        SafeMode = safeMode;
    }

    public DatabaseConnectionData WithSchemaName(string schemaName)
    {
        return new DatabaseConnectionData(Id, CredentialsSource, Credentials.WithSchemaName(schemaName), ConnectionName, Icon, DefaultExpandSchemas, Color, VisibleSchemas, SafeMode);
    }

    public DatabaseConnectionData WithCredentials(DatabaseCredentials credentials)
    {
        return new DatabaseConnectionData(Id, CredentialsSource, credentials, ConnectionName, Icon, DefaultExpandSchemas, Color, VisibleSchemas, SafeMode);
    }

    public DatabaseConnectionData WithId(Guid newGuid)
    {
        return new DatabaseConnectionData(newGuid, CredentialsSource, Credentials, ConnectionName, Icon, DefaultExpandSchemas, Color, VisibleSchemas, SafeMode);
    }
}