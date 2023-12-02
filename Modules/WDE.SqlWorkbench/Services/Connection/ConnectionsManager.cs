using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Settings;

namespace WDE.SqlWorkbench.Services.Connection;

[AutoRegister]
[SingleInstance]
internal class ConnectionsManager : IConnectionsManager
{
    private readonly ISqlWorkbenchPreferences preferences;
    private readonly IMySqlConnector connector;

    private IReadOnlyList<IConnection> connections = null!;
    
    public IReadOnlyList<IConnection> Connections
    {
        get => connections;
        set
        {
            connections = value;
            preferences.Connections = value.Select(x => x.ConnectionData).ToArray();
        }
    }

    public IConnection? DefaultConnection
    {
        get => Connections.FirstOrDefault(c => c.ConnectionData.Id == preferences.DefaultConnection);
        set
        {
            if (value != null)
                preferences.DefaultConnection = value.ConnectionData.Id;
            else
                preferences.DefaultConnection = null;
        }
    }

    public ConnectionsManager(IWorldDatabaseSettingsProvider worldSettings,
        IHotfixDatabaseSettingsProvider hotfixSettings,
        IAuthDatabaseSettingsProvider authSettings,
        ISqlWorkbenchPreferences preferences,
        IMySqlConnector connector)
    {
        this.preferences = preferences;
        this.connector = connector;
        HashSet<DatabaseCredentials> knownCredentials = new();
        List<DatabaseConnectionData> connectionsList = new();

        void AddConnection(IDbAccess dbAccess, CredentialsSource source)
        {
            if (dbAccess.IsEmpty)
                return;
            
            var credentials = new DatabaseCredentials(dbAccess.User ?? "", dbAccess.Password ?? "", dbAccess.Host ?? "", dbAccess.Port ?? 0, "");
            if (!knownCredentials.Add(credentials))
                return;
            
            connectionsList.Add(new DatabaseConnectionData(
                Guid.NewGuid(),
                source,
                credentials.WithSchemaName(dbAccess.Database ?? ""),
                $"{source} @ {dbAccess.Host}",
                new ImageUri("Icons/icon_world.png"),
                false,
                null,
                null));
        }
        
        AddConnection(worldSettings.Settings, CredentialsSource.WorldTable);
        AddConnection(hotfixSettings.Settings, CredentialsSource.HotfixTable);
        AddConnection(authSettings.Settings, CredentialsSource.AuthTable);
        var savedConnections = preferences.Connections.ToList();

        for (var i = 0; i < savedConnections.Count; i++)
        {
            var saveConnection = savedConnections[i];
            if (saveConnection.CredentialsSource != CredentialsSource.Custom)
            {
                connectionsList.RemoveIf(x => x.CredentialsSource == saveConnection.CredentialsSource);
                DatabaseCredentials credentials;
                switch (saveConnection.CredentialsSource)
                {
                    case CredentialsSource.WorldTable:
                        credentials = DatabaseCredentials.FromDbAccess(worldSettings.Settings);
                        break;
                    case CredentialsSource.AuthTable:
                        credentials = DatabaseCredentials.FromDbAccess(authSettings.Settings);
                        break;
                    case CredentialsSource.HotfixTable:
                        credentials = DatabaseCredentials.FromDbAccess(hotfixSettings.Settings);
                        break;
                    case CredentialsSource.Custom:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                savedConnections[i] = saveConnection.WithCredentials(credentials.WithSchemaName(saveConnection.Credentials.SchemaName));
            }
        }

        connectionsList.AddRange(savedConnections);
        Connections = connectionsList.Select(x => new Connection(connector, x)).ToList();
        DefaultConnection ??= Connections.FirstOrDefault();
    }
}