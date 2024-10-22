using System;
using System.Collections.Generic;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Providers
{
    [AutoRegister]
    public class EditorViewMenuItemProvider: IMainMenuItem
    {
        public string ItemName { get; } = "_View";
        public List<IMenuItem> SubItems { get; }
        public MainMenuItemSortPriority SortPriority { get; } = MainMenuItemSortPriority.PriorityHigh;

        public EditorViewMenuItemProvider(IDocumentManager documentManager, 
            Func<QuickStartViewModel> quickStartCreator,
            IGameViewService gameViewService,
            IClippy clippy)
        {
            SubItems = new List<IMenuItem>(documentManager.AllTools.Count);
            SubItems.Add(new ModuleMenuItem("Open quick start", new DelegateCommand(() =>
            {
                documentManager.OpenDocument(quickStartCreator());
            })));
            SubItems.Add(new ModuleMenuItem("Open game view", new DelegateCommand(gameViewService.Open)));
            SubItems.Add(new ModuleManuSeparatorItem());
            foreach (var window in documentManager.AllTools)
            {
                SubItems.Add(new ModuleMenuItem(window.Title, new DelegateCommand(() => documentManager.OpenTool(window.GetType()))));
            }
            SubItems.Add(new ModuleManuSeparatorItem());
            SubItems.Add(new ModuleMenuItem("Open assistant", new DelegateCommand(clippy.Open)));
        }
    }
}