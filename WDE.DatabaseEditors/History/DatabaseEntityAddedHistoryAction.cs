using WDE.Common.History;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;

namespace WDE.DatabaseEditors.History
{
    public class DatabaseEntityAddedHistoryAction : IHistoryAction
    {
        private readonly DatabaseEntity entity;
        private readonly int index;
        private readonly ViewModelBase viewModel;

        public DatabaseEntityAddedHistoryAction(DatabaseEntity entity, int index,
            ViewModelBase viewModel)
        {
            this.entity = entity;
            this.index = index;
            this.viewModel = viewModel;
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
            return $"Entity {entity.Key} added";
        }
    }
    
    public class DatabaseEntityRemovedHistoryAction : IHistoryAction
    {
        private readonly DatabaseEntity entity;
        private readonly int index;
        private readonly ViewModelBase viewModel;

        public DatabaseEntityRemovedHistoryAction(DatabaseEntity entity, int index,
            ViewModelBase viewModel)
        {
            this.entity = entity;
            this.index = index;
            this.viewModel = viewModel;
        }
        
        public void Undo()
        {
            viewModel.ForceInsertEntity(entity, index);
        }

        public void Redo()
        {
            viewModel.ForceRemoveEntity(entity);
        }

        public string GetDescription()
        {
            return $"Entity {entity.Key} removed";
        }
    }
}