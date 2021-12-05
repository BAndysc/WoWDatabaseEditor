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
using WDE.MySqlDatabaseCommon.Providers;
using WDE.Trinity;
using WDE.TrinityMySqlDatabase;
using WDE.TrinityMySqlDatabase.Data;

namespace DatabaseTester;

public class Program
{
    public static int Test<T>(string[] args, params ICoreVersion[] cores) where T : class, IDatabaseProvider
    {
        if (args.Length < 6)
        {
            Console.WriteLine("Usage: ./DatabaseTester [host] [port] [user] [password] [database] [core]");
            Console.WriteLine("Cores: " + string.Join(", ", cores.Select(c => c.Tag)));
            return -1;
        }

        var core = cores.First(c => c.Tag == args[^1]);

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


        var worldDb = ioc.Resolve<T>();

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
    
    public static int Main(string[] args)
    {
        return Test<WorldDatabaseProvider>(args, new AzerothCoreVersion(), new TrinityCataclysmVersion(), new TrinityMasterVersion(), new TrinityWrathVersion());
    }
}