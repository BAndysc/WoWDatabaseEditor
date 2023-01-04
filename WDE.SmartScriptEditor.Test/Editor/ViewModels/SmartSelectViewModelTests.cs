using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WDE.Conditions.Data;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Services;

namespace WDE.SmartScriptEditor.Test.Editor.ViewModels;

public class SmartSelectViewModelTests
{
    
    public class ManualSynchronizationContext : SynchronizationContext
    {
        private List<(SendOrPostCallback d, object state)> queue = new();
    
        public override void Post(SendOrPostCallback d, object state) => queue.Add((d, state));

        public void ExecuteAll()
        {
            var copy = queue.ToArray();
            queue.Clear();
            foreach (var (d, state) in copy)
                d(state);
        }
    }
    
    [Test]
    public async Task Test_TooFastUsers()
    {
        var context = new ManualSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);
        var smartDataManager = Substitute.For<ISmartDataManager>();
        var conditionDataManager = Substitute.For<IConditionDataManager>();
        var favouriteSmartService = Substitute.For<IFavouriteSmartsService>();

        smartDataManager.GetGroupsData(SmartType.SmartAction)
            .Returns(new SmartGroupsJsonData[]
            {
                new()
                {
                    Members = new List<string>(){"SMART_ACTION_A", "SMART_ACTION_B"},
                    Name = "group"
                }
            });
        smartDataManager.Contains(SmartType.SmartAction, "SMART_ACTION_A").Returns(true);
        smartDataManager.Contains(SmartType.SmartAction, "SMART_ACTION_B").Returns(true);
        
        smartDataManager.GetDataByName(SmartType.SmartAction, "SMART_ACTION_A")
            .Returns(new SmartGenericJsonData()
            {
                Name = "SMART_ACTION_A",
                Description = "desc",
                NameReadable = "a"
            });
        smartDataManager.GetDataByName(SmartType.SmartAction, "SMART_ACTION_B")
            .Returns(new SmartGenericJsonData()
            {
                Name = "SMART_ACTION_B",
                Description = "desc",
                NameReadable = "b"
            });
        
        var vm = new SmartSelectViewModel("-", SmartType.SmartAction, _ => true,
            null, smartDataManager, conditionDataManager, favouriteSmartService);
        vm.SearchBox = "a";
        context.ExecuteAll();
        Thread.Sleep(100);
        context.ExecuteAll();
        
        Assert.AreEqual(1, vm.Items.Count(x => x.ShowItem));
    }
}