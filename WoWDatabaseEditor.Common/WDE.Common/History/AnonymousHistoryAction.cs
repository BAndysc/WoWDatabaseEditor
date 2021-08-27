using System;

namespace WDE.Common.History
{
    public class AnonymousHistoryAction : IHistoryAction
    {
        private readonly string description;
        private readonly Action undo;
        private readonly Action redo;

        public AnonymousHistoryAction(string description, System.Action undo, System.Action redo)
        {
            this.description = description;
            this.undo = undo;
            this.redo = redo;
        }

        public void Undo() => undo();

        public void Redo() => redo();

        public string GetDescription() => description;
    }
}