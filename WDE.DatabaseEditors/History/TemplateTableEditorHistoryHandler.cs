using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels.Template;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.History
{
    public class TemplateTableEditorHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly TemplateDbTableEditorViewModel viewModel;
        private IDisposable? disposable;
        
        public TemplateTableEditorHistoryHandler(TemplateDbTableEditorViewModel viewModel)
        {
            this.viewModel = viewModel;
            BindTableData();
        }

        public IDisposable BulkEdit(string name) => WithinBulk(name);

        public void Dispose()
        {
            UnbindTableData();
        }

        private void BindTableData()
        {
            disposable = viewModel.EntitiesObservable[0].ToStream(true).SubscribeAction(e =>
            {
                if (e.Type == CollectionEventType.Add)
                {
                    e.Item.OnAction += PushAction;
                    e.Item.OnConditionsChanged += OnConditionsChanged;
                    e.Item.OnMangosConditionsChanged += OnMangosConditionsChanged;
                    e.Item.OnMangosUnitConditionsChanged += OnMangosUnitConditionsChanged;
                    PushAction(new TemplateDatabaseEntityAddedHistoryAction(e.Item, e.Index, viewModel));
                }
                else if (e.Type == CollectionEventType.Remove)
                {
                    PushAction(new TemplateDatabaseEntityRemovedHistoryAction(e.Item, e.Index, viewModel));
                    e.Item.OnAction -= PushAction;
                    e.Item.OnConditionsChanged -= OnConditionsChanged;
                    e.Item.OnMangosConditionsChanged -= OnMangosConditionsChanged;
                    e.Item.OnMangosUnitConditionsChanged -= OnMangosUnitConditionsChanged;
                }
            });
        }

        private void OnConditionsChanged(DatabaseEntity entity, IReadOnlyList<ICondition>? old, IReadOnlyList<ICondition>? @new)
        {
            PushAction(new DatabaseEntityConditionsChangedHistoryAction(entity, old, @new, viewModel));
        }

        private void OnMangosConditionsChanged(DatabaseEntity entity, Data.Structs.ColumnFullName column, MangosConditionsChange? old, MangosConditionsChange? @new)
        {
            PushAction(new DatabaseEntityMangosConditionsChangedHistoryAction(entity, column, old, @new, viewModel));
        }

        private void OnMangosUnitConditionsChanged(DatabaseEntity entity, Data.Structs.ColumnFullName column, MangosUnitConditionChange? old, MangosUnitConditionChange? @new)
        {
            PushAction(new DatabaseEntityMangosUnitConditionsChangedHistoryAction(entity, column, old, @new, viewModel));
        }
        
        private void UnbindTableData()
        {
            disposable?.Dispose();
            disposable = null;
        }
    }
}