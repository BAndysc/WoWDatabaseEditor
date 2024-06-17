using System.Reactive.Disposables;
using System.Reflection;
using NSubstitute;
using Prism.Events;
using Prism.Ioc;
using Unity;
using WDE.AzerothCore;
using WDE.CMaNGOS;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Factories;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Services;
using WDE.Module;
using WDE.MySqlDatabaseCommon.Database;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;
using WDE.QueryGenerators;
using WDE.QueryGenerators.Base;
using WDE.SqlInterpreter;
using WDE.SqlQueryGenerator;
using WDE.Trinity;
using WDE.TrinityMySqlDatabase;
using WDE.TrinityMySqlDatabase.Data;

namespace DatabaseTester;

public class Program
{
    public class SyncMainThread : IMainThread
    {
        public System.IDisposable Delay(Action action, TimeSpan delay)
        {
            Console.Write(" -- no waiting -- ");
            action();
            return Disposable.Empty;
        }

        public void Dispatch(Action action)
        {
            action();
        }

        public Task Dispatch(Func<Task> action)
        {
            action().Wait();
            return Task.CompletedTask;
        }

        public IDisposable StartTimer(Func<bool> action, TimeSpan interval)
        {
            throw new Exception("Operation not supported");
        }
    }

    private static bool TryGetDefaultObjectForType(Type type, out object? obj)
    {
        obj = null;
        if (type == typeof(string))
            obj = "a";
        else if (type.IsGenericType &&
                 type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                 TryGetDefaultObjectForType(type.GetGenericArguments()[0], out var innerNullable))
        {
            obj = innerNullable;
        }
        else if (type.IsGenericType &&
                 type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) &&
                 TryGetDefaultObjectForType(type.GetGenericArguments()[0], out var inner))
        {
            var listType = typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]);
            var list = Activator.CreateInstance(listType);
            listType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance)!.Invoke(
                list, new object?[]{inner});
            obj = list;
        }
        else if (type.IsValueType)
            obj = Activator.CreateInstance(type);
        return obj != null;
    }

    private static bool TryGetDefaultParameterArray(MethodInfo method, out object?[] arguments)
    {
        var parameters = method.GetParameters();
        arguments = new object?[parameters.Length];
        
        for (int i = 0; i < parameters.Length; ++i)
        {
            if (!TryGetDefaultObjectForType(parameters[i].ParameterType, out var obj))
                return false;
            arguments[i] = obj;
        }

        return true;
    }
    
    public static async Task<int> Test<T>(string[] args, params ICoreVersion[] cores) where T : class, ICachedDatabaseProvider
    {
        if (args.Length < 7)
        {
            Console.WriteLine("Usage: ./DatabaseTester [host] [port] [user] [password] [database] [hotfix database] [core]");
            Console.WriteLine("Cores: " + string.Join(", ", cores.Select(c => c.Tag)));
            return -1;
        }

        var core = cores.First(c => c.Tag == args[^1]);
        
        Console.WriteLine($" -- TESTING {core} -- ");

        var dbSettings = Substitute.For<IWorldDatabaseSettingsProvider>();
        dbSettings.Settings.ReturnsForAnyArgs(new DbAccess()
        {
            Database = args[^3],
            Host = args[^7],
            Password = args[^4],
            Port = int.Parse(args[^6]),
            User = args[^5]
        });
        var hotfixDbSettings = Substitute.For<IHotfixDatabaseSettingsProvider>();
        hotfixDbSettings.Settings.ReturnsForAnyArgs(new DbAccess()
        {
            Database = args[^2],
            Host = args[^7],
            Password = args[^4],
            Port = int.Parse(args[^6]),
            User = args[^5]
        });

        var currentCoreVersion = Substitute.For<ICurrentCoreVersion>();
        currentCoreVersion.Current.ReturnsForAnyArgs(core);

        var databaseConn = Substitute.For<IMySqlWorldConnectionStringProvider>();
        var databaseName = dbSettings.Settings.Database;
        var connString = $"Server={dbSettings.Settings.Host};Port={dbSettings.Settings.Port ?? 3306};Database={dbSettings.Settings.Database};Uid={dbSettings.Settings.User};Pwd={dbSettings.Settings.Password};AllowUserVariables=True;TreatTinyAsBoolean=False";
        databaseConn.ConnectionString.ReturnsForAnyArgs(connString);
        databaseConn.DatabaseName.ReturnsForAnyArgs(databaseName);
        
        
        var ioc = new UnityContainer().AddExtension(new Diagnostic());

        var queryGenerators = typeof(T).Assembly.GetTypes()
            .Where(type => type.GetInterfaces().Where(i => i.IsGenericType)
                .Select(i => i.GetGenericTypeDefinition())
                .Any(i => i == typeof(IInsertQueryProvider<>) ||
                          i == typeof(IDeleteQueryProvider<>) ||
                          i == typeof(IUpdateQueryProvider<>)));
        foreach (var queryGenerator in queryGenerators)
        {
            foreach (var @interface in queryGenerator.GetInterfaces())
                ioc.RegisterSingleton(@interface, queryGenerator);
        }
        
        ioc.RegisterInstance<IContainerProvider>(new UnityContainerProvider(ioc));
        ioc.RegisterInstance<IMessageBoxService>(new ConsoleMessageBoxService());
        ioc.RegisterInstance<IWorldDatabaseSettingsProvider>(dbSettings);
        ioc.RegisterInstance<IHotfixDatabaseSettingsProvider>(hotfixDbSettings);
        ioc.RegisterInstance<IAuthDatabaseSettingsProvider>(Substitute.For<IAuthDatabaseSettingsProvider>());
        ioc.RegisterInstance<ICurrentCoreVersion>(currentCoreVersion);
        ioc.RegisterInstance<ILoadingEventAggregator>(Substitute.For<ILoadingEventAggregator>());
        ioc.RegisterInstance<IEventAggregator>(new EventAggregator());
        ioc.RegisterInstance<ITaskRunner>(new SyncTaskRunner());
        ioc.RegisterInstance<IStatusBar>(Substitute.For<IStatusBar>());
        ioc.RegisterInstance<IParameterFactory>(Substitute.For<IParameterFactory>());
        ioc.RegisterInstance<IUserSettings>(Substitute.For<IUserSettings>());

        ioc.RegisterInstance<IMySqlWorldConnectionStringProvider>(databaseConn);
        ioc.RegisterInstance<IMainThread>(new SyncMainThread());
        ioc.RegisterSingleton<DatabaseLogger>();
        ioc.RegisterInstance<IQueryEvaluator>(Substitute.For<IQueryEvaluator>());
        
        ioc.RegisterSingleton<IMySqlExecutor, WorldMySqlExecutor>();
        ioc.RegisterSingleton<IMySqlHotfixExecutor, NullHotfixMysqlExecutor>();
        ioc.RegisterSingleton<IDatabaseTableDataProvider, DatabaseTableDataProvider>();
        ioc.RegisterSingleton<IDatabaseTableModelGenerator, DatabaseTableModelGenerator>();
        ioc.RegisterSingleton<IDatabaseFieldFactory, DatabaseFieldFactory>();
        ioc.RegisterSingleton<IDatabaseQueryExecutor, DatabaseQueryExecutor>();
        
        ioc.RegisterSingleton<ITableDefinitionDeserializer, TableDefinitionDeserializer>();
        ioc.RegisterSingleton<ITableDefinitionJsonProvider, TableDefinitionJsonProvider>();
        ioc.RegisterSingleton<ITableDefinitionProvider, TableDefinitionProvider>();

        var queryGeneratorModule = new QueryGeneratorModule();
        queryGeneratorModule.InitializeCore(core.Tag);
        queryGeneratorModule.RegisterTypes(new UnityContainerRegistry(ioc, new UnityContainerExtension(ioc)));
        
        var worldDb = ioc.Resolve<T>();
        ioc.RegisterInstance<IDatabaseProvider>(worldDb);
        ioc.RegisterInstance<ICachedDatabaseProvider>(worldDb);

        var allDefinitions = ioc.Resolve<ITableDefinitionProvider>().Definitions;
        var loader = ioc.Resolve<IDatabaseTableDataProvider>();
        var sqlExecutor = ioc.Resolve<WorldMySqlExecutor>();
        foreach (var definition in allDefinitions)
        {
            Console.WriteLine("Table editor: " + definition.TableName);
            
            
            var columnsByTables = definition.Groups.SelectMany(g => g.Fields)
                .Where(g => g.IsActualDatabaseColumn)
                .GroupBy(g => g.ForeignTable ?? definition.TableName)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var table in columnsByTables)
            {
                var tableColumns = await sqlExecutor.GetTableColumns(table.Key);
                var columnsByName = tableColumns
                    .GroupBy(g => g.ColumnName)
                    .ToDictionary(g => g.Key, g => g.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);
            
                foreach (var column in columnsByName.Keys)
                {
                    if (table.Value.Find(t => t.DbColumnName.Equals(column, StringComparison.OrdinalIgnoreCase)) == null &&
                        (definition.ForeignTable == null ||
                         definition.ForeignTable.FirstOrDefault(f => f.TableName == table.Key) is { } x &&
                         !(x.ForeignKeys?.Contains(
                             new ColumnFullName(null, column)) ?? false)))
                    {

                        var top = FuzzySharp.Process.ExtractTop(column,
                                table.Value.Select(x => x.DbColumnName).ToList(), limit: 1)
                            .FirstOrDefault()?.Value;

                        var didYouMean = top != null ? " did you mean: " + top : "";

                        Console.WriteLine($" [ ERROR ] Column {table.Key}.{column} not found in definition!   " + didYouMean);
                    }
                }

                bool anySuperfluous = false;
                foreach (var definitionColumn in table.Value)
                {
                    if (!columnsByName.ContainsKey(definitionColumn.DbColumnName))
                    {
                        var top = FuzzySharp.Process.ExtractTop(definitionColumn.DbColumnName,
                                columnsByName.Values.Select(x => x.ColumnName).ToList(), limit: 1)
                            .FirstOrDefault()?.Value;
                        var didYouMean = top != null ? " did you mean: " + top : "";
                        Console.WriteLine($" [ ERROR ] Column {table.Key}.{definitionColumn.DbColumnName} in definition but NOT in database!   " + didYouMean);
                        anySuperfluous = true;
                    }
                }

                if (anySuperfluous)
                {
                    Console.WriteLine("   did you mean: " + string.Join(", ", columnsByName.Keys));
                }
            }
            
            await loader.Load(definition.Id, null, null, null, new[]{new DatabaseKey(definition.GroupByKeys.Select(x => 1L))});
        }

        var tester = ioc.Resolve<QueryGeneratorTester>();
        var executor = ioc.Resolve<IMySqlExecutor>();
        foreach (var tableName in tester.Tables().Where(x => x != null))
        {
            var result = await executor.ExecuteSelectSql($"SHOW TABLES LIKE '{tableName}'");
            if (result.Rows == 0)
                continue;
            await executor.ExecuteSql($"ALTER TABLE `{tableName}` ROW_FORMAT = DEFAULT");
            await executor.ExecuteSql($"ALTER TABLE `{tableName}` ENGINE = InnoDB");
        }

        foreach (var queryGenerator in tester.Generate())
        {
            IQuery query;
            try
            {
                query = queryGenerator();
            }
            catch (TableNotSupportedException e)
            {
                Console.WriteLine("Skipping table: " + e.TableName + " because it is not supported by the selected core.");
                continue;
            }
            
            try
            {
                await executor.ExecuteSql(query, true);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                Console.WriteLine(query);
                return -1;
            }
        }
        
        var allMethods = typeof(T).GetMethods();
        HashSet<string> methodsHandledManually = new HashSet<string>()
        {
            "Equals",
            "FindEventScriptLinesBy",
            "FindSmartScriptLinesBy",
            "GetConditionsForAsync",
            "GetEventScript",
            "GetCreaturesAsync",
            "GetGameObjectsAsync"
        };

        foreach (var method in allMethods)
        {
            if (methodsHandledManually.Contains(method.Name))
                continue;
            
            object? ret = null;
            if (TryGetDefaultParameterArray(method, out var arguments))
            {
                Console.WriteLine(method.Name);
                ret = method.Invoke(worldDb, arguments);
            }
            else
            {
                var p = method.GetParameters();
                var types = string.Join(", ", p.Select(x => x.ParameterType));
                Console.WriteLine($"Skipping method: {method.Name}({types}), due to unsupported parameters");
            }
            
            if (ret is Task t)
                await t;
        }

        // methods with > 1 parameters
        await worldDb.GetScriptForAsync(0, 0, SmartScriptType.Creature);
        await worldDb.GetConditionsForAsync(0, 0, 0);
        await worldDb.GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask.All, new IDatabaseProvider.ConditionKey(0, 0, 0, 0));
        await worldDb.GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask.All, new List<IDatabaseProvider.ConditionKey>(){new IDatabaseProvider.ConditionKey(0, 0, 0, 0)});
        await worldDb.FindSmartScriptLinesBy(new (IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)[] { (IDatabaseProvider.SmartLinePropertyType.Action, 0, 0, 0) });
        await worldDb.GetCreaturesAsync();
        await worldDb.GetCreaturesAsync(new SpawnKey[]{new SpawnKey(0, 0)});
        await worldDb.GetGameObjectsAsync();
        await worldDb.GetGameObjectsAsync(new SpawnKey[]{new SpawnKey(0, 0)});
        
        foreach (var type in Enum.GetValues<EventScriptType>())
        {
            Console.WriteLine($"GetEventScript({type})");
            await worldDb.GetEventScript(type, 0);
        }
        
        foreach (var type in Enum.GetValues<LootSourceType>())
        {
            Console.Write($"GetLoot({type})... ");
            try
            {
                await worldDb.GetLoot(type, 0);
                Console.WriteLine("OK");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("Not supported (but it's ok)");
            }
        }
        
        return 0;
    }
    
    public static void FixCurrentDirectory()
    {
        var path = Assembly.GetExecutingAssembly().Location;
        if (string.IsNullOrEmpty(path))
            path = System.AppContext.BaseDirectory;
        var exePath = new FileInfo(path);
        if (exePath.Directory != null)
            Directory.SetCurrentDirectory(exePath.Directory.FullName);
    }
    
    public static async Task<int> Main(string[] args)
    {
        FixCurrentDirectory();
        
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments provided");
            return -1;
        }

        if (args[^1] == "CMaNGOS-WoTLK")
            return await DatabaseTester.Program.Test<WDE.CMMySqlDatabase.WorldDatabaseProvider>(args, new CMangosWrathVersion());
        else if (args[^1] == "CMaNGOS-TBC")
            return await DatabaseTester.Program.Test<WDE.CMMySqlDatabase.WorldDatabaseProvider>(args, new CMangosTbcVersion());
        else if (args[^1] == "CMaNGOS-Classic")
            return await DatabaseTester.Program.Test<WDE.CMMySqlDatabase.WorldDatabaseProvider>(args, new CMangosClassicVersion());
        return await Test<WorldDatabaseProvider>(args, new AzerothCoreVersion(), new TrinityCataclysmVersion(), new TrinityMasterVersion(), new TrinityWrathVersion());
    }
}