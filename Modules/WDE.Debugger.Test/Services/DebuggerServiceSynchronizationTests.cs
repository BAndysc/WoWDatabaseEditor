using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Prism.Events;
using WDE.Common.Debugging;
using WDE.Common.Services;
using WDE.Debugger.Services;

namespace WDE.Debugger.Test.Services;

public class DebuggerServiceSynchronizationTests
{
    private class DemoPayload : IDebugPointPayload
    {
        public DemoPayload(int value)
        {
            Value = value;
        }

        public int Value { get; set; }
    }

    private IDebuggerDelayedSaveService delayedSaveService = null!;
    private IUserSettings userSettings = null!;
    private IDebugPointSynchronizer synchronizer = null!;
    private IDebugPointSource source = null!;
    private IRemoteConnectorService remoteConnectorService = null!;
    private IEventAggregator eventAggregator = null!;
    private IDebugPointSource[] sources = null!;
    private DebuggerService service = null!;

    [SetUp]
    public void Setup()
    {
        delayedSaveService = Substitute.For<IDebuggerDelayedSaveService>();
        userSettings = Substitute.For<IUserSettings>();
        synchronizer = Substitute.For<IDebugPointSynchronizer>();
        source = Substitute.For<IDebugPointSource>();
        eventAggregator = Substitute.For<IEventAggregator>();
        source.Synchronizer.Returns(synchronizer);
        remoteConnectorService = Substitute.For<IRemoteConnectorService>();
        sources = new[] { source };
        remoteConnectorService.IsConnected.Returns(true);
        eventAggregator.GetEvent<IdeBreakpointHitEvent>().Returns(new IdeBreakpointHitEvent());
        eventAggregator.GetEvent<IdeBreakpointResumeEvent>().Returns(new IdeBreakpointResumeEvent());
        service = new DebuggerService(remoteConnectorService, userSettings,  delayedSaveService, eventAggregator, sources);
    }

    [Test]
    public async Task Breakpoint_Create_Delete_No_Synchronize()
    {
        bool eventFired = false;
        bool enabled = false;
        bool generateStackTrace = false;
        BreakpointState state = default;
        bool suspendExecution = false;
        bool deleteFired = false;
        service.DebugPointAdded += p =>
        {
            eventFired = true;
            enabled = service.GetEnabled(p);
            generateStackTrace = service.GetGenerateStacktrace(p);
            state = service.GetState(p);
            suspendExecution = service.GetSuspendExecution(p);
        };
        service.DebugPointRemoving += p =>
        {
            // in the remove callback, the payload should be still valid
            Assert.IsTrue(service.TryGetPayload<DemoPayload>(p, out DemoPayload? payload));
            Assert.AreEqual(5, payload.Value);
            deleteFired = true;
        };

        var debugPoint = service.CreateDebugPoint(source, new DemoPayload(5));
        Assert.AreNotEqual(DebugPointId.Empty, debugPoint);
        Assert.IsTrue(eventFired);
        // test default values
        Assert.IsTrue(enabled);
        Assert.IsTrue(generateStackTrace);
        Assert.AreEqual(BreakpointState.Pending, state);
        Assert.IsFalse(suspendExecution);

        // synchronize needs to be explicitly called
        await synchronizer.DidNotReceiveWithAnyArgs().Synchronize(debugPoint);

        await service.RemoveDebugPointAsync(debugPoint, false);

        await synchronizer.Received().Delete(debugPoint);

        Assert.IsTrue(deleteFired);
    }

    [Test]
    public async Task Breakpoint_Exception_In_Synchronize()
    {
        var debugPoint = service.CreateDebugPoint(source, new DemoPayload(5));
        Assert.AreEqual(BreakpointState.Pending, service.GetState(debugPoint));

        synchronizer.Synchronize(debugPoint).ReturnsForAnyArgs(Task.FromException<SynchronizationResult>(new Exception("simulated error")));

        Assert.ThrowsAsync<Exception>(() => service.Synchronize(debugPoint));

        await synchronizer.Received().Synchronize(debugPoint);

        Assert.AreEqual(BreakpointState.SynchronizationError, service.GetState(debugPoint));
    }

    [Test]
    public async Task Breakpoint_Long_Synchronize_And_Delete_Before()
    {
        var debugPoint = service.CreateDebugPoint(source, new DemoPayload(5));
        Assert.AreEqual(BreakpointState.Pending, service.GetState(debugPoint));

        var synchronizeLatch = new TaskCompletionSource<SynchronizationResult>();

        synchronizer.Synchronize(debugPoint).ReturnsForAnyArgs(synchronizeLatch.Task);

        var synchronizeTask = service.Synchronize(debugPoint);

        Assert.AreEqual(BreakpointState.Pending, service.GetState(debugPoint));

        var deleteTask = service.RemoveDebugPointAsync(debugPoint, false);

        synchronizeLatch.SetResult(SynchronizationResult.Ok);

        await synchronizeTask;
        await deleteTask;
        
        Assert.AreEqual(0, service.DebugPoints.Count());
    }

    [Test]
    public async Task Synchronize_UnchangedDebugPoint_DoesNotTriggerSynchronization()
    {
        bool debugPointChangedEventFired = false;
        service.DebugPointChanged += p => debugPointChangedEventFired = true;

        var debugPoint = service.CreateDebugPoint(source, new DemoPayload(10));
        await service.Synchronize(debugPoint); // Assume this triggers the first synchronization

        // Reset state to simulate no changes
        debugPointChangedEventFired = false;
        synchronizer.ClearReceivedCalls();

        // Attempt to synchronize again without making changes
        await service.Synchronize(debugPoint);

        // Verify that synchronization was not triggered again
        await synchronizer.DidNotReceiveWithAnyArgs().Synchronize(debugPoint);
        Assert.IsFalse(debugPointChangedEventFired, "DebugPointChanged event should not fire if the debug point was not modified.");
    }

    [Test]
    public async Task ModifyDebugPoint_AfterSynchronization_RequiresResynchronization()
    {
        bool debugPointChangedEventFired = false;
        service.DebugPointChanged += p => debugPointChangedEventFired = true;

        var debugPoint = service.CreateDebugPoint(source, new DemoPayload(20));
        await service.Synchronize(debugPoint); // Synchronize the debug point initially

        // Modify the debug point, triggering a need for resynchronization
        service.SetLog(debugPoint, "New log message");

        // Verify state change and event firing
        Assert.IsTrue(debugPointChangedEventFired, "Modifying a debug point should trigger the DebugPointChanged event.");
        Assert.AreEqual(BreakpointState.Pending, service.GetState(debugPoint), "Debug point state should be set to Pending after modification.");

        // Verify that synchronization is expected again
        await synchronizer.Received().Synchronize(debugPoint);
    }

    [Test]
    public async Task SynchronizeAndRemoveDebugPoint_Sequence_HandlesCorrectly()
    {
        var removalLatch = new TaskCompletionSource<bool>();
        bool debugPointRemovedEventFired = false;
        service.DebugPointRemoved += p =>
        {
            debugPointRemovedEventFired = true;
        };

        // Create a new debug point
        var debugPoint = service.CreateDebugPoint(source, new DemoPayload(5));

        // Set up the mock to delay deletion until the latch is released
        synchronizer.Delete(debugPoint).Returns(removalLatch.Task);

        // Initiate removal in the background without awaiting completion immediately
        var removeTask = service.RemoveDebugPointAsync(debugPoint, false);

        // Immediately allow the removal operation to proceed, eliminating the wait
        removalLatch.SetResult(true);

        // Then, initiate synchronization
#if DEBUG
        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.Synchronize(debugPoint));
#else
        await service.Synchronize(debugPoint);
#endif
        // Await the removal task to ensure it completes after synchronization
        await removeTask;

        // Ensure the debug point was removed as expected
        Assert.IsTrue(debugPointRemovedEventFired, "DebugPointRemoved event should fire indicating successful removal.");
        // Ensure cleanup actions were performed as expected
        await synchronizer.Received().Delete(debugPoint);
    }
}