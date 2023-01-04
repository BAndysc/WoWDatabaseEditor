using System;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WDE.Profiles.Services;

[AutoRegister]
[SingleInstance]
public class InterEditorIntegration : IGlobalService, IDisposable
{
    private readonly IInterEditorCommunication interEditorCommunication;
    private readonly IProjectItemSerializer serializer;
    private readonly ILoadingEventAggregator loadingEventAggregator;

    public InterEditorIntegration(IInterEditorCommunication interEditorCommunication,
        IWindowManager windowManager, 
        ISolutionItemDeserializerRegistry deserializerRegistry,
        IProjectItemSerializer serializer,
        ILoadingEventAggregator loadingEventAggregator,
        IEventAggregator eventAggregator)
    {
        this.interEditorCommunication = interEditorCommunication;
        this.serializer = serializer;
        this.loadingEventAggregator = loadingEventAggregator;
        interEditorCommunication.OpenRequest.SubscribeAction(projectItem =>
        {
            if (deserializerRegistry.TryDeserialize(projectItem, out var solutionItem))
            {
                eventAggregator.GetEvent<EventRequestOpenItem>().Publish(solutionItem!);
                windowManager.Activate();
            }
        });

        interEditorCommunication.BringToFrontRequest.SubscribeAction(_ =>
        {
            windowManager.Activate();
        });
        
        if (GlobalApplication.Arguments.GetValue("open") is { } openItem)
        {
            if (serializer.TryDeserializeItem(openItem, out var projectItem))
            {
                if (deserializerRegistry.TryDeserialize(projectItem, out var solutionItem))
                {
                    eventAggregator.GetEvent<EventRequestOpenItem>().Publish(solutionItem!);
                }
            }
        }

        loadingEventAggregator.OnEvent<EditorLoaded>().SubscribeAction(_ =>
        {
            if (OperatingSystem.IsWindows() ||
                OperatingSystem.IsLinux() || 
                OperatingSystem.IsMacOS())
                interEditorCommunication.Open();
        });
    }

    public void Dispose()
    {
        interEditorCommunication.Close();
    }
}