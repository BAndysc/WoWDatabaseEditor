using System;
using NSubstitute;
using NSubstitute.Extensions;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;
using WDE.Common.Debugging;
using WDE.Debugger.ViewModels.Inspector;

namespace WDE.Debugger.Test.ViewModels.Inspector;

public class SelectedDebugPointViewModelTests
{
    [Test]
    public void TestIsEnabled()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId(); // Create a specific DebugPointId instance
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId });

        // Act
        viewModel.IsEnabled = true;

        // Assert
        debuggerService.Received(1).SetEnabled(debugPointId, true);
        debuggerService.Received(1).Synchronize(debugPointId);
    }

    [Test]
    public void TestSuspendExecution()
    {
        // Arrange
        var source = Substitute.For<IDebugPointSource>();
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId();
        debuggerService.GetSource(default).ReturnsForAnyArgs(source);
        source.Features.Returns(DebugSourceFeatures.CanChangeSuspendExecution);
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId });

        // Act
        viewModel.SuspendExecution = true;

        // Assert
        debuggerService.Received(1).SetSuspendExecution(debugPointId, true);
        debuggerService.Received(1).Synchronize(debugPointId);
    }

    [Test]
    public void TestLog()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        debuggerService.GetLogFormat(default).ReturnsNullForAnyArgs();
        var debugPointId = new DebugPointId(); // Create a specific DebugPointId instance
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId });

        // Act
        viewModel.Log = true;

        // Assert
        debuggerService.Received(1).SetLog(debugPointId, "");
        debuggerService.Received(1).Synchronize(debugPointId);
    }

    [Test]
    public void TestUseLogCondition()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        debuggerService.GetLogCondition(default).ReturnsNullForAnyArgs();
        var debugPointId = new DebugPointId(); // Create a specific DebugPointId instance
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId });

        // Act
        viewModel.UseLogCondition = true;

        // Assert
        debuggerService.Received(1).SetLogCondition(debugPointId, "");
        debuggerService.Received(1).Synchronize(debugPointId);
    }

    [Test]
    public void TestLogCondition()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId(); // Create a specific DebugPointId instance
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId });

        // Act
        viewModel.LogCondition = "TestCondition";

        // Assert
        debuggerService.Received(1).SetLogCondition(debugPointId, "TestCondition");
    }

    [Test]
    public void TestLogFormat()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId(); // Create a specific DebugPointId instance
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId });

        // Act
        viewModel.LogFormat = "TestFormat";

        // Assert
        debuggerService.Received(1).SetLog(debugPointId, "TestFormat");
        debuggerService.Received(1).Synchronize(debugPointId);
    }

    [Test]
    public void TestNoStacktrace()
    {
        // Arrange
        var source = Substitute.For<IDebugPointSource>();
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId();
        debuggerService.GetSource(default).ReturnsForAnyArgs(source);
        source.Features.Returns(DebugSourceFeatures.CanChangeGenerateStacktrace);
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId });

        // Act
        viewModel.NoStacktrace = false;

        // Assert
        debuggerService.Received(1).SetGenerateStacktrace(debugPointId, true);
        debuggerService.Received(1).Synchronize(debugPointId);
    }

    [Test]
    public void TestPropertiesWithDifferentValuesAndThenSameValue()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var source = Substitute.For<IDebugPointSource>();
        var debugPointId1 = new DebugPointId();
        var debugPointId2 = new DebugPointId(1, 0);
        debuggerService.GetSource(default).ReturnsForAnyArgs(source);
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId1, debugPointId2 });

        // Set different initial values for debug points
        debuggerService.GetEnabled(debugPointId1).Returns(true);
        debuggerService.GetEnabled(debugPointId2).Returns(false);

        debuggerService.GetSuspendExecution(debugPointId1).Returns(true);
        debuggerService.GetSuspendExecution(debugPointId2).Returns(false);

        // Assert initial values
        Assert.IsTrue(viewModel.IsEnabled == null);
        Assert.IsTrue(viewModel.SuspendExecution == null);

        // Act
        viewModel.IsEnabled = true;
        viewModel.SuspendExecution = true;

        // Set same value for all debug points
        debuggerService.GetEnabled(debugPointId1).Returns(true);
        debuggerService.GetEnabled(debugPointId2).Returns(true);

        debuggerService.GetSuspendExecution(debugPointId1).Returns(true);
        debuggerService.GetSuspendExecution(debugPointId2).Returns(true);

        // Assert after setting same value
        Assert.IsTrue(viewModel.IsEnabled == true);
        Assert.IsTrue(viewModel.SuspendExecution == true);
    }

    [Test]
    public void TestDebugPointChangedEvent()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId();
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId });

        bool isDebugEnabledPropertyChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(viewModel.IsEnabled))
                isDebugEnabledPropertyChanged = true;
        };

        // Act
        debuggerService.DebugPointChanged += Raise.Event<Action<DebugPointId>>(debugPointId);

        // Assert
        Assert.IsTrue(isDebugEnabledPropertyChanged);
    }

    [Test]
    public void TestDebugPointChangedEvent_IgnoredForDifferentDebugPoint()
    {
        // Arrange
        var source = Substitute.For<IDebugPointSource>();
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId1 = new DebugPointId(0, 0);
        var debugPointId2 = new DebugPointId(1, 0);
        debuggerService.GetSource(default).ReturnsForAnyArgs(source);
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId1 });

        bool isDebugEnabledPropertyChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(viewModel.IsEnabled))
                isDebugEnabledPropertyChanged = true;
        };

        // Act - invoking DebugPointChanged event for a different debug point
        debuggerService.DebugPointChanged += Raise.Event<Action<DebugPointId>>(debugPointId2);

        // Assert - Ensure the PropertyChanged event wasn't raised
        Assert.IsFalse(isDebugEnabledPropertyChanged);
    }

    [Test]
    public void TestCanChangeSuspendExecution()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId();
        var debugSource = Substitute.For<IDebugPointSource>();
        debugSource.Features.Returns(DebugSourceFeatures.CanChangeSuspendExecution);
        debuggerService.GetSource(debugPointId).Returns(debugSource);
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId });

        // Act & Assert
        Assert.IsTrue(viewModel.CanChangeSuspendExecution);
    }

    [Test]
    public void TestCanChangeGenerateStacktrace()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId();
        var debugSource = Substitute.For<IDebugPointSource>();
        debugSource.Features.Returns(DebugSourceFeatures.CanChangeGenerateStacktrace);
        debuggerService.GetSource(debugPointId).Returns(debugSource);
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId });

        // Act & Assert
        Assert.IsTrue(viewModel.CanChangeGenerateStacktrace);
    }

    [Test]
    public void TestCannotChangeSuspendExecution()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId();
        var debugSource = Substitute.For<IDebugPointSource>();
        debugSource.Features.Returns((DebugSourceFeatures)0);
        debuggerService.GetSource(debugPointId).Returns(debugSource);
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId });

        // Act & Assert
        Assert.IsFalse(viewModel.CanChangeSuspendExecution);

        viewModel.SuspendExecution = true;
        viewModel.SuspendExecution = false;

        debuggerService.DidNotReceiveWithAnyArgs().SetSuspendExecution(default, default);
    }

    [Test]
    public void TestCannotChangeGenerateStacktrace()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId();
        var debugSource = Substitute.For<IDebugPointSource>();
        debugSource.Features.Returns((DebugSourceFeatures)0);
        debuggerService.GetSource(debugPointId).Returns(debugSource);
        var viewModel = new SelectedDebugPointViewModel(debuggerService, new DebugPointId[] { debugPointId });

        // Act & Assert
        Assert.IsFalse(viewModel.CanChangeGenerateStacktrace);

        viewModel.NoStacktrace = true;
        viewModel.NoStacktrace = false;

        debuggerService.DidNotReceiveWithAnyArgs().SetGenerateStacktrace(default, default);
    }
}