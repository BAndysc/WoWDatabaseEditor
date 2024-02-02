using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Debugging;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Debugger.ViewModels.Inspector;

namespace WDE.Debugger.Test.ViewModels.Inspector;

public class DebugPointsInspectorViewModelTests
{
    private DebugPointsInspectorViewModel _viewModel = null!;
    private IRemoteConnectorService _remoteConnectorService = null!;
    private IDebuggerService _debuggerService = null!;
    private IMessageBoxService _messageBoxService = null!;
    private IMainThread _mainThread = null!;
    private IDebugPointSource source = null!;

    [SetUp]
    public void Setup()
    {
        _mainThread = Substitute.For<IMainThread>();
        GlobalApplication.InitializeApplication(_mainThread, GlobalApplication.AppBackend.Avalonia);
        _remoteConnectorService = Substitute.For<IRemoteConnectorService>();
        _debuggerService = Substitute.For<IDebuggerService>();
        _messageBoxService = Substitute.For<IMessageBoxService>();
        source = Substitute.For<IDebugPointSource>();
        _debuggerService.Sources.Returns(new List<IDebugPointSource> { source });

        _viewModel = new DebugPointsInspectorViewModel(_remoteConnectorService, _debuggerService, _messageBoxService);
    }

    [TearDown]
    public void TearDown()
    {
        GlobalApplication.Deinitialize();
    }

    [Test]
    public async Task OnDebugPointAdded_ShouldUpdateSourcesAndFirePropertyChanged()
    {
        var newDebugPointId = new DebugPointId(0, 0);

        var tcs = new TaskCompletionSource<bool>();
        _debuggerService.GetSource(newDebugPointId).Returns(source);
        _debuggerService.DebugPoints.Returns(new List<DebugPointId> { newDebugPointId });

        Assert.AreEqual(1, _viewModel.FlatItems.Count);

        _debuggerService.DebugPointAdded += Raise.Event<Action<DebugPointId>?>(newDebugPointId);

        Assert.AreEqual(2, _viewModel.FlatItems.Count);
    }

    [Test]
    public async Task AddDebugPointCommand_ShouldCreateDebugPoint()
    {
        // Arrange
        var mockSource = Substitute.For<IDebugPointSource>();
        mockSource.Features.Returns(DebugSourceFeatures.CanCreateDebugPoint); // Assuming CanCreateDebugPoint is a flag for sources that can create debug points
        _debuggerService.Sources.Returns(new List<IDebugPointSource> { mockSource });

        // Re-initialize the ViewModel to pick up the mocked sources
        _viewModel = new DebugPointsInspectorViewModel(_remoteConnectorService, _debuggerService, _messageBoxService);

        var addDebugPointViewModel = _viewModel.SourceAddList.First();

        // Act
        await _viewModel.AddDebugPointCommand.ExecuteAsync(addDebugPointViewModel);

        // Assert
        await mockSource.Received(1).CreateDebugPoint();
    }

    [Test]
    public async Task DeleteSelectedDebugPointsCommand_WhenSelectedItemIsNull_ShouldNotExecute()
    {
        _viewModel.SelectedNode = null;
        Assert.IsFalse(_viewModel.DeleteSelectedDebugPointsCommand.CanExecute(null));

        await _viewModel.DeleteSelectedDebugPointsCommand.ExecuteAsync();

        await _debuggerService.DidNotReceiveWithAnyArgs().RemoveDebugPointAsync(default);
    }

    [Test]
    public async Task DeleteSelectedDebugPointsCommand_WhenDeleteFails_ShouldShowMessageBox()
    {
        var selectedDebugPointId = new DebugPointId(0, 0);
        _viewModel.SelectedNode = new InspectorDebugPointViewModel(selectedDebugPointId, _debuggerService);
        _debuggerService.RemoveDebugPointAsync(Arg.Any<DebugPointId>()).Returns(Task.FromException(new Exception("Test exception")));

        await _viewModel.DeleteSelectedDebugPointsCommand.ExecuteAsync();

        await _messageBoxService.Received(1).ShowDialog(Arg.Any<IMessageBox<bool>>());
    }
}