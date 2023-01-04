using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using Prism.Events;
using WDE.Common.Debugging;
using WDE.Common.Services;
using WDE.Debugger.Services;

namespace WDE.Debugger.Test.Services
{
    [TestFixture]
    public class DebuggerServiceTests
    {
        private DebuggerService service = null!;
        private IRemoteConnectorService remoteConnectorService = null!;
        private IUserSettings userSettings = null!;
        private IDebuggerDelayedSaveService delayedSaveService = null!;
        private IEventAggregator eventAggregator = null!;
        private List<IDebugPointSource> sources = null!;

        public class TestDebugPointPayload : IDebugPointPayload
        {
            public string SomeProperty { get; }

            public TestDebugPointPayload(string someProperty)
            {
                SomeProperty = someProperty;
            }
        }

        [SetUp]
        public void Setup()
        {
            remoteConnectorService = Substitute.For<IRemoteConnectorService>();
            userSettings = Substitute.For<IUserSettings>();
            eventAggregator = Substitute.For<IEventAggregator>();
            delayedSaveService = Substitute.For<IDebuggerDelayedSaveService>();
            sources = new List<IDebugPointSource>();

            for (int i = 0; i < 3; i++)
            {
                var source = Substitute.For<IDebugPointSource>();
                source.Key.Returns($"Source{i}");
                sources.Add(source);
            }

            remoteConnectorService.IsConnected.Returns(true);
            eventAggregator.GetEvent<IdeBreakpointHitEvent>().Returns(new IdeBreakpointHitEvent());
            eventAggregator.GetEvent<IdeBreakpointResumeEvent>().Returns(new IdeBreakpointResumeEvent());
            service = new DebuggerService(remoteConnectorService, userSettings, delayedSaveService, eventAggregator, sources);
        }

        [Test]
        public void CreateDebugPoint_ValidParameters_ShouldCreateDebugPoint()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();

            // Act
            var debugPointId = service.CreateDebugPoint(source, payload);

            // Assert
            Assert.NotNull(debugPointId);
            Assert.IsTrue(service.HasDebugPoint(debugPointId));
        }

        [Test]
        public async Task RemoveDebugPointAsync_ValidId_RemoteNotRemoved_ShouldSuccessfullyRemove()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            Assert.IsTrue(service.HasDebugPoint(debugPointId));

            // Act
            await service.RemoveDebugPointAsync(debugPointId, false);

            // Assert
            Assert.IsFalse(service.HasDebugPoint(debugPointId));
        }

        [Test]
        public async Task ClearAllDebugPoints_WithExistingDebugPoints_ShouldClearAll()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            service.CreateDebugPoint(source, payload);

            // Act
            await service.ClearAllDebugPoints();

            // Assert
            Assert.IsEmpty(service.DebugPoints);
        }

        [Test]
        public void SetLog_ValidId_ShouldUpdateLog()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            var newLog = "UpdatedLog";

            // Act
            service.SetLog(debugPointId, newLog);

            // Assert
            var log = service.GetLogFormat(debugPointId);
            Assert.AreEqual(newLog, log);
        }

// Additional test methods for DebuggerServiceTests

        [Test]
        public void SetEnabled_ValidId_ShouldUpdateEnabledState()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            var newState = !service.GetEnabled(debugPointId);

            // Act
            service.SetEnabled(debugPointId, newState);

            // Assert
            // Assuming a method or a way to verify the state, since it's internal logic not directly accessible
            Assert.AreEqual(newState,
                service.GetEnabled(debugPointId)); // Assuming GetEnabled method for demonstration
        }

        [Test]
        public void SetActivated_ValidId_ShouldUpdateActivatedState()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            var newState = !service.GetActivated(debugPointId);

            // Act
            service.SetActivated(debugPointId, newState);

            // Assert
            Assert.AreEqual(newState,
                service.GetActivated(debugPointId)); // Assuming GetActivated method for demonstration
        }

        [Test]
        public void SetSuspendExecution_ValidId_ShouldUpdateSuspendExecution()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            var newState = !service.GetSuspendExecution(debugPointId);

            // Act
            service.SetSuspendExecution(debugPointId, newState);

            // Assert
            Assert.AreEqual(newState,
                service.GetSuspendExecution(debugPointId)); // Assuming GetSuspendExecution method for demonstration
        }

        [Test]
        public void SetGenerateStacktrace_ValidId_ShouldUpdateGenerateStacktrace()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            var newState = !service.GetGenerateStacktrace(debugPointId);

            // Act
            service.SetGenerateStacktrace(debugPointId, newState);

            // Assert
            Assert.AreEqual(newState,
                service.GetGenerateStacktrace(
                    debugPointId)); // Assuming GetGenerateStacktrace method for demonstration
        }

        [Test]
        public void SetLogCondition_ValidId_ShouldUpdateLogCondition()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            var newCondition = "newCondition";

            // Act
            service.SetLogCondition(debugPointId, newCondition);

            // Assert
            var condition = service.GetLogCondition(debugPointId);
            Assert.AreEqual(newCondition, condition);
        }

        [Test]
        public async Task Synchronize_DebugPointPendingState_ShouldAttemptSynchronization()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            source.Synchronizer.Synchronize(debugPointId).Returns(Task.FromResult(SynchronizationResult.Ok));

            // Act
            await service.Synchronize(debugPointId);

            // Assert
            Assert.AreEqual(BreakpointState.Synced, service.GetState(debugPointId)); // Assuming GetState method for demonstration
        }

        [Test]
        public void Synchronize_ThrowsException_ShouldSetStateToError()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            source.Synchronizer.Synchronize(debugPointId).Returns(Task.FromException<SynchronizationResult>(new Exception("Synchronization failed")));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await service.Synchronize(debugPointId));
            Assert.AreEqual(BreakpointState.SynchronizationError, service.GetState(debugPointId)); // Assuming GetState method for demonstration
        }

        [Test]
        public async Task RemoveDebugPointAsync_RemoteAlreadyRemoved_ShouldRemoveWithoutSynchronization()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            // Simulate that the remote has already removed the debug point
            var remoteAlreadyRemoved = true;

            // Act
            await service.RemoveDebugPointAsync(debugPointId, remoteAlreadyRemoved);

            // Assert
            Assert.IsFalse(service.HasDebugPoint(debugPointId));
        }

        [Test]
        public async Task RemoveIfAsync_MatchesCondition_ShouldRemoveDebugPoint()
        {
            // Arrange
            var source = sources.First();
            var matchingPayload = new TestDebugPointPayload("Match");
            var nonMatchingPayload = new TestDebugPointPayload("NoMatch");
            var matchingDebugPointId = service.CreateDebugPoint(source, matchingPayload);
            service.CreateDebugPoint(source, nonMatchingPayload);

            // Act
            await service.RemoveIfAsync<TestDebugPointPayload>(p => p.SomeProperty == "Match", false);

            // Assert
            Assert.IsFalse(service.HasDebugPoint(matchingDebugPointId));
            // Assuming there's a way to get count or list all debug points for assertion
            Assert.AreEqual(1, service.DebugPoints.Count());
        }

        [Test]
        public void TryGetPayload_ValidId_ReturnsTrueAndPayload()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);

            // Act
            var result = service.TryGetPayload(debugPointId, out IDebugPointPayload retrievedPayload);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(payload, retrievedPayload);
        }

        [Test]
        public async Task PropertyChangeDuringSynchronization_ShouldQueueCorrectly()
        {
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);

            var tcsInitialSync = new TaskCompletionSource<SynchronizationResult>();
            var tcsSecondSync = new TaskCompletionSource<SynchronizationResult>();

            source.Synchronizer.Synchronize(Arg.Is(debugPointId)).Returns(tcsInitialSync.Task, tcsSecondSync.Task);

            var initialSyncTask = service.Synchronize(debugPointId);

            service.SetEnabled(debugPointId, false);

            var secondSyncTask = service.Synchronize(debugPointId);

            tcsInitialSync.SetResult(SynchronizationResult.Ok);
            await initialSyncTask;

            tcsSecondSync.SetResult(SynchronizationResult.Ok);
            await secondSyncTask;

            Assert.AreEqual(BreakpointState.Synced, service.GetState(debugPointId));
        }

        [Test]
        public async Task SynchronizationCancellation_ShouldCancelGracefully()
        {
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);

            var tcsSync = new TaskCompletionSource<SynchronizationResult>();
            source.Synchronizer.Synchronize(Arg.Is(debugPointId)).Returns(tcsSync.Task);

            var syncTask = service.Synchronize(debugPointId);
            tcsSync.SetCanceled();

            try
            {
                await syncTask;
            }
            catch (TaskCanceledException)
            {
                // Expected
            }

            // Verifying state after cancellation could depend on how cancellation is handled in your logic.
            // This assertion is here as a placeholder; adjust according to your method's behavior.
            Assert.AreEqual(BreakpointState.Pending, service.GetState(debugPointId));
        }

        [Test]
        public async Task Synchronization_WithException_ShouldHandleError()
        {
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);

            var tcsSync = new TaskCompletionSource<SynchronizationResult>();
            source.Synchronizer.Synchronize(Arg.Is(debugPointId)).Returns(tcsSync.Task);

            var exception = new Exception("Synchronization failed");
            var syncTask = service.Synchronize(debugPointId);
            tcsSync.SetException(exception);

            try
            {
                await syncTask;
                Assert.Fail("Expected an exception to be thrown.");
            }
            catch (Exception ex)
            {
                Assert.AreEqual(exception, ex);
            }

            Assert.AreEqual(BreakpointState.SynchronizationError, service.GetState(debugPointId));
        }

        [Test]
        public async Task SetProperty_WithExceptionDuringSynchronization_ShouldUpdateState()
        {
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);

            var tcsSync = new TaskCompletionSource<SynchronizationResult>();
            source.Synchronizer.Synchronize(Arg.Is(debugPointId)).Returns(async _ => throw new Exception("Synchronization failed"));

            // Trigger property update that requires synchronization
            service.SetEnabled(debugPointId, true);
            Assert.ThrowsAsync<Exception>(async () => await service.Synchronize(debugPointId));

            // Assuming a mechanism to check if the synchronization attempt was made and failed
            Assert.AreEqual(BreakpointState.SynchronizationError, service.GetState(debugPointId));
        }

        [Test]
        public async Task FetchExisting_WithExceptions_ShouldHandleGracefully()
        {
            var source = sources.First();
            source.FetchFromServerAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new Exception("Fetch failed")));

            remoteConnectorService.EditorConnected += Raise.Event<Action>();
        }

        [Test]
        public void SaveDebugPoints_ShouldCorrectlySerializeAndSave()
        {
            delayedSaveService.When(x => x.ScheduleSave(Arg.Any<Action>())).Do(callback =>
            {
                var action = callback.Arg<Action>();
                action.Invoke(); // Execute the save action immediately
            });

            // Arrange
            var debugPointSource = sources.First();
            var debugPointPayload = Substitute.For<IDebugPointPayload>();
            JObject expectedPayload = new JObject { ["key"] = "value" };
            debugPointSource.SerializePayload(Arg.Any<DebugPointId>()).Returns(expectedPayload);

            var debugPointId = service.CreateDebugPoint(debugPointSource, debugPointPayload);

            // Assert
            userSettings.Received(1).Update(Arg.Is<DebuggerService.DebugPointsSnapshot>(snapshots =>
                snapshots.Snapshots.Any(s => s.SourceName == debugPointSource.Key && s.Payload != null)));
        }

        [Test]
        public void LoadDebugPoints_ShouldCorrectlyDeserializeAndLoad()
        {
            // Arrange
            var expectedSourceKey = sources.First().Key;
            var debugPointsSnapshot = new DebuggerService.DebugPointsSnapshot
            {
                Snapshots = new List<DebuggerService.DebugPointSnapshot>
                {
                    new DebuggerService.DebugPointSnapshot
                    {
                        SourceName = expectedSourceKey,
                        Activated = true,
                        Enabled = true,
                        Log = "Test Log",
                        Payload = new JObject { ["key"] = "value" } // Simulate a serialized payload
                    }
                }
            };

            userSettings.Get<DebuggerService.DebugPointsSnapshot>().Returns(debugPointsSnapshot);

            // Mock the DeserializePayload method to return a specific payload object when called
            var expectedPayload = Substitute.For<IDebugPointPayload>();
            sources.First().DeserializePayload(Arg.Any<JObject>()).Returns(expectedPayload);

            // Act
            service.LoadDebugPoints(); // Load points at service initialization or through a specific call

            // Assert
            Assert.IsTrue(service.DebugPoints.Any()); // Verify that debug points were loaded

            // Assuming you have a way to access loaded debug points, further assertions can be made here.
            // For example, verifying that the deserialized payload matches the expected payload object:
            service.TryGetPayload(service.DebugPoints.First(), out IDebugPointPayload actualPayload);
            Assert.AreEqual(expectedPayload, actualPayload);

            // Additional assertions can verify the properties (Activated, Enabled, Log) of the loaded debug points.
            var debugPoint = service.DebugPoints.First();
            Assert.AreEqual(true, service.GetActivated(debugPoint));
            Assert.AreEqual(true, service.GetEnabled(debugPoint));
            Assert.AreEqual("Test Log", service.GetLogFormat(debugPoint));
        }

        [Test]
        public void LoadDebugPoints_WithNonExistingSource_ShouldGracefullyHandle()
        {
            // Arrange
            var nonExistingSourceKey = "NonExistingSource";
            var debugPointsSnapshot = new DebuggerService.DebugPointsSnapshot
            {
                Snapshots = new List<DebuggerService.DebugPointSnapshot>
                {
                    new DebuggerService.DebugPointSnapshot
                    {
                        SourceName = nonExistingSourceKey,
                        Activated = true,
                        Enabled = true,
                        Log = "Test Log",
                        Payload = new JObject { ["key"] = "value" } // Simulate a serialized payload
                    }
                }
            };

            userSettings.Get<DebuggerService.DebugPointsSnapshot>().Returns(debugPointsSnapshot);

            // No logger is involved in this scenario

            // Act & Assert
            Assert.DoesNotThrow(() => service.LoadDebugPoints()); // Verify loading process does not throw

            // Further verification that no debug points are loaded due to the non-existing source
            Assert.IsFalse(service.DebugPoints.Any(), "No debug points should be loaded for non-existing sources.");
        }

        [Test]
        public void LoadDebugPoints_WithNullPayload_ShouldHandleGracefully()
        {
            // Arrange
            var existingSourceKey = sources.First().Key;
            var debugPointsSnapshot = new DebuggerService.DebugPointsSnapshot
            {
                Snapshots = new List<DebuggerService.DebugPointSnapshot>
                {
                    new DebuggerService.DebugPointSnapshot
                    {
                        SourceName = existingSourceKey,
                        Activated = true,
                        Enabled = true,
                        Log = "Test Log",
                        Payload = null // Simulate a scenario with a null payload
                    }
                }
            };

            userSettings.Get<DebuggerService.DebugPointsSnapshot>().Returns(debugPointsSnapshot);

            // Mock the DeserializePayload method to handle null payload appropriately
            // Depending on implementation, this could return a default payload, throw an error, or simply skip
            var defaultPayload = Substitute.For<IDebugPointPayload>();
            sources.First().DeserializePayload(Arg.Any<JObject>()).Returns(ci =>
                ci.Arg<JObject>() == null ? defaultPayload : Substitute.For<IDebugPointPayload>());

            // Act
            Assert.DoesNotThrow(() => service.LoadDebugPoints()); // Verify loading process does not throw

            // Assert
            // Ensure the service handled the null payload without throwing exceptions
            // This could mean verifying a default payload was used, or simply that no errors occurred
            Assert.IsFalse(service.DebugPoints.Any(), "Debug points should not be loaded with null payload.");
            // Further assertions can be made depending on expected behavior: skip, use default payload, etc.
        }

        [Test]
        public async Task Breakpoint_Synchronize_ReturnsOutOfSync()
        {
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);

            // Mock the Synchronizer to return OutOfSync when synchronizing this debug point
            source.Synchronizer.Synchronize(debugPointId).Returns(Task.FromResult(SynchronizationResult.OutOfSync));

            bool debugPointChangedFired = false;
            service.DebugPointChanged += id =>
            {
                if (id == debugPointId)
                {
                    debugPointChangedFired = true;
                }
            };

            // Act
            await service.Synchronize(debugPointId);

            // Assert
            Assert.IsTrue(debugPointChangedFired, "Expected DebugPointChanged to be fired.");
            var state = service.GetState(debugPointId);
            Assert.AreEqual(BreakpointState.WaitingForSync, state, "Expected debug point state to be OutOfSync.");
        }

        [Test]
        public async Task ServerDisconnected_EventFired_ShouldSetDebugPointsToPending()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);

            // Initially, synchronize the debug point to set a state other than Pending
            source.Synchronizer.Synchronize(debugPointId).Returns(Task.FromResult(SynchronizationResult.Ok));
            await service.Synchronize(debugPointId);
            var state = service.GetState(debugPointId);
            Assert.AreEqual(BreakpointState.Synced, state);

            // Act
            // Simulate firing the EditorDisconnected event
            remoteConnectorService.EditorDisconnected += Raise.Event<Action>();

            // Assert
            state = service.GetState(debugPointId);
            Assert.AreEqual(BreakpointState.Pending, state, "All debug points should be set to Pending after server disconnection.");
        }

        [Test]
        public async Task EditorConnected_EventFired_ShouldAutomaticallySynchronizeDebugPoints()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);

            // Assume the debug point is initially out of sync to verify synchronization behavior upon connection
            source.Synchronizer.Synchronize(debugPointId).Returns(Task.FromResult(SynchronizationResult.OutOfSync));

            bool synchronizationAttempted = false;
            source.Synchronizer.When(x => x.Synchronize(Arg.Any<DebugPointId>())).Do(_ => synchronizationAttempted = true);

            // Act
            // Simulate firing the EditorConnected event
            remoteConnectorService.EditorConnected += Raise.Event<Action>();

            // Small delay to allow any asynchronous operations to complete
            await Task.Delay(100);

            // Assert
            Assert.IsTrue(synchronizationAttempted, "Synchronization should be attempted for all debug points upon editor connection.");

            // Additional checks can be made to ensure the state of the debug point is updated accordingly,
            // but this requires either direct access to check the state or observing effects that imply synchronization.
        }

        [Test]
        public async Task EditorConnected_EventFired_ShouldSynchronizeAppropriateDebugPointsAndHandleExceptions()
        {
            // Assume _sources has at least three sources initialized in the Setup method
            var source1 = sources[0]; // For the debug point to be synchronized
            var source2 = sources[1]; // For the enabled debug point, not to be synchronized
            var source3 = sources[2]; // For the debug point that throws an exception during synchronization

            var payload = Substitute.For<IDebugPointPayload>();

            // Debug point that should be synchronized
            var syncDebugPointId = service.CreateDebugPoint(source1, payload);

            // Enabled debug point, not to be synchronized
            var enabledDebugPointId = service.CreateDebugPoint(source2, payload);
            service.SetEnabled(enabledDebugPointId, false);

            // Debug point that will throw an exception during synchronization
            var exceptionDebugPointId = service.CreateDebugPoint(source3, payload);

            // Setup synchronization behaviors
            bool syncSynchronizationAttempted = false;
            source1.Synchronizer.Synchronize(syncDebugPointId).Returns(_ =>
            {
                syncSynchronizationAttempted = true;
                return Task.FromResult(SynchronizationResult.Ok);
            });

            bool disabledSynchronizationAttempted = false;
            source2.Synchronizer.Synchronize(enabledDebugPointId).Returns(_ =>
            {
                disabledSynchronizationAttempted = true;
                return Task.FromResult(SynchronizationResult.Ok);
            });

            bool exceptionSynchronizationAttempted = false;
            source3.Synchronizer.Synchronize(exceptionDebugPointId).Returns<Task<SynchronizationResult>>(async _ =>
            {
                exceptionSynchronizationAttempted = true;
                throw new Exception("Synchronization failed.");
            });

            // Act
            // Simulate firing the EditorConnected event
            remoteConnectorService.EditorConnected += Raise.Event<Action>();

            await Task.Delay(100); // Allow time for async operations initiated by the event to run

            // Assert
            Assert.IsFalse(disabledSynchronizationAttempted, "Enabled debug point should not attempt synchronization.");
            Assert.IsTrue(syncSynchronizationAttempted, "Synchronization should be attempted for the sync debug point.");
            Assert.IsTrue(exceptionSynchronizationAttempted, "Exception debug point should attempt synchronization but fail.");

            // The test ensures that the correct debug points attempt synchronization based on their configured state and source,
            // and that an exception during synchronization of one does not prevent or affect the synchronization of others.
        }

        [Test]
        public async Task CreatingDebugPoint_AfterRemovingPreviousOne_YieldsDifferentDebugPointId()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();

            // Create a debug point and then remove it
            var debugPointId1 = service.CreateDebugPoint(source, payload);
            await service.RemoveDebugPointAsync(debugPointId1, false);

            // Act
            // Create another debug point after removing the first one
            var debugPointId2 = service.CreateDebugPoint(source, payload);

            // Assert
            Assert.AreNotEqual(debugPointId1, debugPointId2, "The new debug point ID should be different from the removed debug point ID.");

            // Optionally, verify that the newly created debug point has a valid state and is correctly initialized
            var state = service.GetState(debugPointId2);
            Assert.AreEqual(BreakpointState.Pending, state, "Newly created debug point should be in the Pending state.");
        }

        [Test]
        public void GetSource_ValidDebugPointId_ReturnsCorrectSource()
        {
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);

            var resultSource = service.GetSource(debugPointId);

            Assert.AreEqual(source, resultSource, "The returned source should match the source of the created debug point.");
        }

        [Test]
        public async Task ForceSetPending_ValidDebugPoint_ShouldUpdateStateAndFireEvent()
        {
            bool eventFired = false;
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            service.DebugPointChanged += id => eventFired = id == debugPointId;

            service.ForceSetPending(debugPointId);

            var state = service.GetState(debugPointId);
            Assert.AreEqual(BreakpointState.Pending, state, "Debug point state should be set to Pending.");
            Assert.IsTrue(eventFired, "DebugPointChanged event should be fired after setting to Pending.");
            await source.Synchronizer.DidNotReceive().Synchronize(default);
        }

        [Test]
        public async Task ForceSetSynchronized_ValidDebugPoint_ShouldUpdateStateAndFireEvent()
        {
            bool eventFired = false;
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            service.DebugPointChanged += id => eventFired = id == debugPointId;

            remoteConnectorService.IsConnected.Returns(false);
            service.ForceSetSynchronized(debugPointId);

            var state = service.GetState(debugPointId);
            Assert.AreEqual(BreakpointState.Pending, state, "ForceSetSynchronized ignored if remote is not connected.");

            remoteConnectorService.IsConnected.Returns(true);
            service.ForceSetSynchronized(debugPointId);

            state = service.GetState(debugPointId);
            Assert.AreEqual(BreakpointState.Synced, state, "Debug point state should be set to Synced.");
            Assert.IsTrue(eventFired, "DebugPointChanged event should be fired after setting to Synced.");
            await source.Synchronizer.DidNotReceive().Synchronize(default);
        }

        [Test]
        public async Task SetPayload_ValidDebugPoint_ShouldUpdatePayloadAndFireEvent()
        {
            bool eventFired = false;
            var source = sources.First();
            var newPayload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, Substitute.For<IDebugPointPayload>());
            service.DebugPointChanged += id => eventFired = id == debugPointId;

            service.SetPayload(debugPointId, newPayload);

            service.TryGetPayload(debugPointId, out IDebugPointPayload resultPayload);
            Assert.AreEqual(newPayload, resultPayload, "Debug point payload should be updated.");
            Assert.IsTrue(eventFired, "DebugPointChanged event should be fired after payload update.");
            await source.Synchronizer.DidNotReceive().Synchronize(default);
        }
        [Test]
        public void TryFindDebugPoint_WithMatchingCondition_ShouldReturnTrueAndCorrectId()
        {
            var source = sources.First();
            var payload1 = new TestDebugPointPayload("a");
            var payload2 = new TestDebugPointPayload("b");
            service.CreateDebugPoint(source, payload1);
            var debugPointId2 = service.CreateDebugPoint(source, payload2);

            bool found = service.TryFindDebugPoint<TestDebugPointPayload>(p => p.SomeProperty == "b", out DebugPointId resultId);

            Assert.IsTrue(found, "Should find the debug point with matching condition.");
            Assert.AreEqual(debugPointId2, resultId, "The found debug point ID should match the expected debug point ID.");
        }

        [Test]
        public async Task Synchronize_DisabledPoint_DeleteCanceled_ShouldHandleGracefully()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            service.SetEnabled(debugPointId, false); // Disable the debug point

            var taskCompletionSource = new TaskCompletionSource<SynchronizationResult>();
            source.Synchronizer.Delete(Arg.Any<DebugPointId>()).Returns(async ci =>
            {
                await taskCompletionSource.Task;
            });

            var syncTask = service.Synchronize(debugPointId);
            taskCompletionSource.SetCanceled();
            await syncTask;
            var state = service.GetState(debugPointId);
            Assert.AreEqual(BreakpointState.Pending, state, "Debug point state should remain Pending after cancellation.");
        }

        [Test]
        public async Task Synchronize_DisabledPoint_DeleteThrowsException_ShouldHandleGracefully()
        {
            // Arrange
            var source = sources.First();
            var payload = Substitute.For<IDebugPointPayload>();
            var debugPointId = service.CreateDebugPoint(source, payload);
            service.SetEnabled(debugPointId, false); // Disable the debug point

            var exceptionMessage = "Deletion failed";
            source.Synchronizer.Delete(Arg.Any<DebugPointId>()).Returns(Task.FromException(new Exception(exceptionMessage)));

            // Act
            Exception? caughtException = null;
            try
            {
                await service.Synchronize(debugPointId);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException, "An exception should have been caught.");
            Assert.AreEqual(exceptionMessage, caughtException!.Message, "The caught exception should match the expected message.");

            // Verify state after exception
            var state = service.GetState(debugPointId);
            Assert.AreEqual(BreakpointState.SynchronizationError, state, "Debug point state should be SynchronizationError after an exception.");
        }
    }
}