using System;
using System.Collections.Generic;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.QuickAccess;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Services;

namespace WDE.QuestChainEditor;

[AutoRegisterToParentScope]
[SingleInstance]
public class QuestsChainTopBarQuickAccessProvider : ITopBarQuickAccessProvider
{
    public IEnumerable<ITopBarQuickAccessItem> Items { get; }
    public int Order => 0;

    public QuestsChainTopBarQuickAccessProvider(Lazy<IStandaloneQuestChainEditorService> chainEditorService)
    {
        Items = new ITopBarQuickAccessItem[]
        {
            new TopBarQuickAccessItem("Quest Chains", new ImageUri("Icons/document_quest_chain.png"),
                new DelegateCommand(
                    () =>
                    {
                        chainEditorService.Value.OpenStandaloneEditor();
                    }))
        };
    }

    private class TopBarQuickAccessItem : ITopBarQuickAccessItem
    {
        public TopBarQuickAccessItem(string name, ImageUri icon, ICommand command)
        {
            Name = name;
            Icon = icon;
            Command = command;
        }

        public ICommand Command { get; }
        public string Name { get; }
        public ImageUri Icon { get; }
    }
}