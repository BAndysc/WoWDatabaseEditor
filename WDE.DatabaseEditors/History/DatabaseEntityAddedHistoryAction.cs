using WDE.Common.History;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;

namespace WDE.DatabaseEditors.History
{
    public class TemplateDatabaseEntityAddedHistoryAction : IHistoryAction
    {
        private readonly DatabaseEntity entity;
        private readonly int index;
        private readonly ViewModelBase viewModel;
        private readonly DatabaseKey actualKey;

        public TemplateDatabaseEntityAddedHistoryAction(DatabaseEntity entity, int index,
            ViewModelBase viewModel)
        {
            this.entity = entity;
            this.index = index;
            this.viewModel = viewModel;
            actualKey = entity.GenerateKey(viewModel.TableDefinition);
        }
        
        public void Undo()
        {
            viewModel.ForceRemoveEntity(entity);
        }

        public void Redo()
        {
            viewModel.ForceInsertEntity(entity, index);
        }

        public string GetDescription()
        {
            return $"Entity {actualKey} added";
        }
    }
    
    public class TemplateDatabaseEntityRemovedHistoryAction : IHistoryAction
    {
        private readonly DatabaseEntity entity;
        private readonly int index;
        private readonly ViewModelBase viewModel;
        private readonly DatabaseKey actualKey;

        public TemplateDatabaseEntityRemovedHistoryAction(DatabaseEntity entity, int index,
            ViewModelBase viewModel)
        {
            this.entity = entity;
            this.index = index;
            this.viewModel = viewModel;
            actualKey = entity.GenerateKey(viewModel.TableDefinition);
        }
        
        public void Undo()
        {
            viewModel.ForceInsertEntity(entity, index, true);
        }

        public void Redo()
        {
            viewModel.ForceRemoveEntity(entity);
        }

        public string GetDescription()
        {
            return $"Entity {actualKey} removed";
        }
    }
}