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
using WDE.DatabaseEditors.Factories;
using WDE.DatabaseEditors.Loaders;
using WDE.MySqlDatabaseCommon.Database;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;
using WDE.QueryGenerators;
using WDE.SqlInterpreter;
using WDE.Trinity;
using WDE.TrinityMySqlDatabase;
using WDE.TrinityMySqlDatabase.Data;

namespace DatabaseTester;

public class Program
{
    public class SyncMainThread : IMainThread
    {
        public void Delay(Action action, TimeSpan delay)
        {
            Console.Write(" -- no waiting -- ");
            action();
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
    
    public static async Task<int> Test<T>(string[] args, params ICoreVersion[] cores) where T : class, IDatabaseProvider
    {
        if (args.Length < 6)
        {
            Console.WriteLine("Usage: ./DatabaseTester [host] [port] [user] [password] [database] [core]");
            Console.WriteLine("Cores: " + string.Join(", ", cores.Select(c => c.Tag)));
            return -1;
        }

        var core = cores.First(c => c.Tag == args[^1]);
        
        Console.WriteLine($" -- TESTING {core} -- ");

        var dbSettings = Substitute.For<IWorldDatabaseSettingsProvider>();
        dbSettings.Settings.ReturnsForAnyArgs(new DbAccess()
        {
            Database = args[^2],
            Host = args[^6],
            Password = args[^3],
            Port = int.Parse(args[^5]),
            User = args[^4]
        });

        var currentCoreVersion = Substitute.For<ICurrentCoreVersion>();
        currentCoreVersion.Current.ReturnsForAnyArgs(core);

        var databaseConn = Substitute.For<IMySqlWorldConnectionStringProvider>();
        var connString = $"Server={dbSettings.Settings.Host};Port={dbSettings.Settings.Port ?? 3306};Database={dbSettings.Settings.Database};Uid={dbSettings.Settings.User};Pwd={dbSettings.Settings.Password};AllowUserVariables=True";
        databaseConn.ConnectionString.ReturnsForAnyArgs(connString);
        
        var ioc = new UnityContainer().AddExtension(new Diagnostic());
        ioc.RegisterInstance<IContainerProvider>(new UnityContainerProvider(ioc));
        ioc.RegisterInstance<IMessageBoxService>(new ConsoleMessageBoxService());
        ioc.RegisterInstance<IWorldDatabaseSettingsProvider>(dbSettings);
        ioc.RegisterInstance<IAuthDatabaseSettingsProvider>(Substitute.For<IAuthDatabaseSettingsProvider>());
        ioc.RegisterInstance<ICurrentCoreVersion>(currentCoreVersion);
        ioc.RegisterInstance<ILoadingEventAggregator>(Substitute.For<ILoadingEventAggregator>());
        ioc.RegisterInstance<IEventAggregator>(new EventAggregator());
        ioc.RegisterInstance<ITaskRunner>(new SyncTaskRunner());
        ioc.RegisterInstance<IStatusBar>(Substitute.For<IStatusBar>());
        ioc.RegisterInstance<IParameterFactory>(Substitute.For<IParameterFactory>());

        ioc.RegisterInstance<IMySqlWorldConnectionStringProvider>(databaseConn);
        ioc.RegisterInstance<IMainThread>(new SyncMainThread());
        ioc.RegisterSingleton<DatabaseLogger>();
        ioc.RegisterInstance<IQueryEvaluator>(Substitute.For<IQueryEvaluator>());
        
        ioc.RegisterSingleton<IMySqlExecutor, MySqlExecutor>();
        ioc.RegisterSingleton<IDatabaseTableDataProvider, DatabaseTableDataProvider>();
        ioc.RegisterSingleton<IDatabaseTableModelGenerator, DatabaseTableModelGenerator>();
        ioc.RegisterSingleton<IDatabaseFieldFactory, DatabaseFieldFactory>();
        
        ioc.RegisterSingleton<ITableDefinitionDeserializer, TableDefinitionDeserializer>();
        ioc.RegisterSingleton<ITableDefinitionJsonProvider, TableDefinitionJsonProvider>();
        ioc.RegisterSingleton<ITableDefinitionProvider, TableDefinitionProvider>();

        var worldDb = ioc.Resolve<T>();
        ioc.RegisterInstance<IDatabaseProvider>(worldDb);

        var module = new QueryGeneratorModule();
        module.InitializeCore(core.Tag);
        module.RegisterTypes(new UnityContainerRegistry(ioc));

        var allDefinitions = ioc.Resolve<ITableDefinitionProvider>().Definitions;
        var loader = ioc.Resolve<IDatabaseTableDataProvider>();
        foreach (var definition in allDefinitions)
        {
            Console.WriteLine("Table editor: " + definition.TableName);
            await loader.Load(definition.Id, null, null, null, new[]{new DatabaseKey(definition.GroupByKeys.Select(x => 1L))});
        }

        var tester = ioc.Resolve<QueryGeneratorTester>();
        var executor = ioc.Resolve<IMySqlExecutor>();
        foreach (var tableName in tester.Tables().Where(x => x != null))
        {
            await executor.ExecuteSql($"ALTER TABLE `{tableName}` ROW_FORMAT = DEFAULT");
            await executor.ExecuteSql($"ALTER TABLE `{tableName}` ENGINE = InnoDB");
        }

        foreach (var query in tester.Generate().Where(x => x != null))
        {
            try
            {
                await executor.ExecuteSql(query!);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                Console.WriteLine(query);
                return -1;
            }
        }
        
        var allMethods = typeof(T).GetMethods();
        foreach (var method in allMethods)
        {
            var p = method.GetParameters();

            object? ret = null;
            if (p.Length == 0)
            {
                Console.WriteLine(method.Name);
                ret = method.Invoke(worldDb, new object?[] { });
            }
            else if (p.Length == 1 && p[0].ParameterType == typeof(uint))
            {
                Console.WriteLine(method.Name);
                ret = method.Invoke(worldDb, new object?[] { (uint)0 });
            }
            else if (p.Length == 1 && p[0].ParameterType == typeof(string))
            {
                Console.WriteLine(method.Name);
                ret = method.Invoke(worldDb, new object?[] { "" });
            }

            if (ret is Task t)
                await t;
        }

        // methods with > 1 parameters
        worldDb.GetScriptFor(0, SmartScriptType.Creature);
        worldDb.GetConditionsFor(0, 0, 0);
        return 0;
    }
    
    private static void FixCurrentDirectory()
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