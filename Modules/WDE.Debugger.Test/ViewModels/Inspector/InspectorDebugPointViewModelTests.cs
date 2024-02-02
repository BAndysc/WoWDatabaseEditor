using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Debugging;
using WDE.Common.Utils;
using WDE.Debugger.ViewModels.Inspector;

namespace WDE.Debugger.Test.ViewModels.Inspector;

public class InspectorDebugPointViewModelTests
{
    [Test]
    public void TestIsEnabled()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId();
        var viewModel = new InspectorDebugPointViewModel(debugPointId, debuggerService);

        // Act
        viewModel.IsEnabled = true;

        // Assert
        debuggerService.Received(1).SetEnabled(debugPointId, true);
        debuggerService.Received(1).Synchronize(debugPointId).ListenErrors();
    }

    [Test]
    public void TestRaiseChanged()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId();
        var viewModel = new InspectorDebugPointViewModel(debugPointId, debuggerService);
        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (sender, args) =>
        {
            changedProperties.Add(args.PropertyName!);
        };

        // Act
        viewModel.RaiseChanged();

        // Assert
        var props = typeof(InspectorDebugPointViewModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(x => x.Name)
            .Except(typeof(INodeType).GetProperties().
                Select(x => x.Name))
            .Except(new []{nameof(InspectorDebugPointViewModel.Id)})
            .ToList();
        CollectionAssert.AreEquivalent(props, changedProperties);
        Assert.Contains(nameof(InspectorDebugPointViewModel.SuspendExecution), changedProperties);
        Assert.Contains(nameof(InspectorDebugPointViewModel.State), changedProperties);
        Assert.Contains(nameof(InspectorDebugPointViewModel.IsConnected), changedProperties);
        Assert.Contains(nameof(InspectorDebugPointViewModel.IsEnabled), changedProperties);
        Assert.Contains(nameof(InspectorDebugPointViewModel.IsDeactivated), changedProperties);
        Assert.Contains(nameof(InspectorDebugPointViewModel.Header), changedProperties);
    }

    [Test]
    public void TestPropertyAccessorsCallDebuggerServiceMethods()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId();
        var viewModel = new InspectorDebugPointViewModel(debugPointId, debuggerService);

        // Act
        _ = viewModel.IsEnabled;
        _ = viewModel.IsDeactivated;
        _ = viewModel.SuspendExecution;
        _ = viewModel.State;
        _ = viewModel.Header;

        // Assert
        debuggerService.Received(1).GetEnabled(debugPointId);
        debuggerService.Received(1).GetActivated(debugPointId);
        debuggerService.Received(1).GetSuspendExecution(debugPointId);
        debuggerService.Received(1).GetState(debugPointId);
        debuggerService.Received(1).GetSource(debugPointId).Received(1).GenerateName(debugPointId);
    }

    [Test]
    public void TestPropertyAccessorsCallDebuggerServiceMethodsValues()
    {
        // Arrange
        var debuggerService = Substitute.For<IDebuggerService>();
        var debugPointId = new DebugPointId();
        var viewModel = new InspectorDebugPointViewModel(debugPointId, debuggerService);
        const bool expectedIsEnabled = true;
        const bool expectedIsDeactivated = false;
        const bool expectedSuspendExecution = true;
        const BreakpointState expectedState = BreakpointState.Synced;
        const string expectedHeader = "Test Header";
        debuggerService.GetEnabled(debugPointId).Returns(expectedIsEnabled);
        debuggerService.GetActivated(debugPointId).Returns(!expectedIsDeactivated);
        debuggerService.GetSuspendExecution(debugPointId).Returns(expectedSuspendExecution);
        debuggerService.GetState(debugPointId).Returns(expectedState);
        debuggerService.GetSource(debugPointId).GenerateName(debugPointId).Returns(expectedHeader);
        viewModel.RaiseChanged();

        // Act
        var isEnabled = viewModel.IsEnabled;
        var isDeactivated = viewModel.IsDeactivated;
        var suspendExecution = viewModel.SuspendExecution;
        var state = viewModel.State;
        var header = viewModel.Header;

        // Assert
        Assert.AreEqual(expectedIsEnabled, isEnabled);
        Assert.AreEqual(expectedIsDeactivated, isDeactivated);
        Assert.AreEqual(expectedSuspendExecution, suspendExecution);
        Assert.AreEqual(expectedState, state);
        Assert.AreEqual(expectedHeader, header);
    }
}