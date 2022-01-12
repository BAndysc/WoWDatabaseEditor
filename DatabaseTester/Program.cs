using System.Reflection;
using NSubstitute;
using Prism.Events;
using Prism.Ioc;
using Unity;
using WDE.AzerothCore;
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
    }
    
    public static int Test<T>(string[] args, params ICoreVersion[] cores) where T : class, IDatabaseProvider
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
        
        var ioc = new UnityContainer();
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

        var allDefinitions = ioc.Resolve<ITableDefinitionProvider>().Definitions;
        var loader = ioc.Resolve<IDatabaseTableDataProvider>();
        foreach (var definition in allDefinitions)
        {
            Console.WriteLine("Table editor: " + definition.TableName);
            var task = loader.Load(definition.Id, 1);
            task.Wait();
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
                t.Wait();
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
    
    public static int Main(string[] args)
    {
        FixCurrentDirectory();
        return Test<WorldDatabaseProvider>(args, new AzerothCoreVersion(), new TrinityCataclysmVersion(), new TrinityMasterVersion(), new TrinityWrathVersion());
    }
}