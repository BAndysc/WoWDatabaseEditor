using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public ObservableCollection<IConnection> AllConnections { get; } = new();

    public IReadOnlyList<IConnection> StaticConnections
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
        get => StaticConnections.FirstOrDefault(c => c.ConnectionData.Id == preferences.DefaultConnection);
        set
        {
            if (value != null)
                preferences.DefaultConnection = value.ConnectionData.Id;
            else
                preferences.DefaultConnection = null;
        }
    }

    public IConnection Clone(IConnection baseConnection, string schemaName)
    {
        var newConnection = new Connection(connector, baseConnection.ConnectionData.WithId(Guid.NewGuid()).WithSchemaName(schemaName));
        AllConnections.Add(newConnection);
        return newConnection;
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
                null,
                QueryExecutionSafety.ExecuteAll));
        }
        
        AddConnection(worldSettings.Settings, CredentialsSource.WorldDatabase);
        AddConnection(hotfixSettings.Settings, CredentialsSource.HotfixDatabase);
        AddConnection(authSettings.Settings, CredentialsSource.AuthDatabase);
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
                    case CredentialsSource.WorldDatabase:
                        credentials = DatabaseCredentials.FromDbAccess(worldSettings.Settings);
                        break;
                    case CredentialsSource.AuthDatabase:
                        credentials = DatabaseCredentials.FromDbAccess(authSettings.Settings);
                        break;
                    case CredentialsSource.HotfixDatabase:
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
        StaticConnections = connectionsList.Select(x => new Connection(connector, x)).ToList();
        AllConnections.AddRange(StaticConnections);
        DefaultConnection ??= StaticConnections.FirstOrDefault();
    }
}