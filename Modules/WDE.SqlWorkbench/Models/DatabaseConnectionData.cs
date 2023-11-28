using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using WDE.Common.Types;

namespace WDE.SqlWorkbench.Models;

internal enum CredentialsSource
{
    Custom,
    WorldTable,
    AuthTable,
    HotfixTable
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

    public bool IsTemporary => Id == Guid.Empty;

    public DatabaseConnectionData(Guid id,
        CredentialsSource credentialsSource,
        DatabaseCredentials credentials,
        string connectionName,
        ImageUri? icon,
        bool defaultExpandSchemas,
        Color? color, 
        string[]? visibleSchemas)
    {
        Credentials = credentials;
        CredentialsSource = credentialsSource;
        ConnectionName = connectionName;
        Id = id;
        Icon = icon;
        DefaultExpandSchemas = defaultExpandSchemas;
        Color = color;
        VisibleSchemas = visibleSchemas?.ToArray();
    }

    public DatabaseConnectionData WithSchemaName(string schemaName)
    {
        return new DatabaseConnectionData(Id, CredentialsSource, Credentials.WithSchemaName(schemaName), ConnectionName, Icon, DefaultExpandSchemas, Color, VisibleSchemas);
    }

    public DatabaseConnectionData WithCredentials(DatabaseCredentials credentials)
    {
        return new DatabaseConnectionData(Id, CredentialsSource, credentials, ConnectionName, Icon, DefaultExpandSchemas, Color, VisibleSchemas);
    }

    public DatabaseConnectionData WithId(Guid newGuid)
    {
        return new DatabaseConnectionData(newGuid, CredentialsSource, Credentials, ConnectionName, Icon, DefaultExpandSchemas, Color, VisibleSchemas);
    }
}