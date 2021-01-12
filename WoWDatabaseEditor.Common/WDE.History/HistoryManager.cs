using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.History;
using WDE.Module.Attributes;

namespace WDE.History
{
    [AutoRegister]
    public class HistoryManager : IHistoryManager, INotifyPropertyChanged
    {
        private readonly Stack<IHistoryAction> future;
        private readonly Stack<IHistoryAction> history;


        private bool acceptNew;
        private bool canRedo;
        private bool canUndo;

        public HistoryManager()
        {
            history = new Stack<IHistoryAction>();
            future = new Stack<IHistoryAction>();
            CanUndo = false;
            CanRedo = false;
            acceptNew = true;
        }

        public ObservableCollection<IHistoryAction> Past { get; } = new();
        public ObservableCollection<IHistoryAction> Future { get; } = new();

        public bool CanUndo
        {
            get => canUndo;
            private set
            {
                canUndo = value;
                OnPropertyChanged();
            }
        }

        public bool CanRedo
        {
            get => canRedo;
            private set
            {
                canRedo = value;
                OnPropertyChanged();
            }
        }

        public int UndoCount => history.Count;
        public int RedoCount => future.Count;

        public void AddHandler(HistoryHandler handler)
        {
            handler.ActionPush += (sender, action) =>
            {
                if (!acceptNew)
                    return;

                history.Push(action);
                Past.Add(action);
                future.Clear();
                Future.Clear();
                CanRedo = false;
                CanUndo = true;
            };
        }

        public void Undo()
        {
            if (history.Count == 0)
                throw new NothingToUndoException();

            IHistoryAction action = history.Pop();
            future.Push(action);
            Past.RemoveAt(Past.Count - 1);
            Future.Insert(0, action);
            acceptNew = false;
            action.Undo();
            acceptNew = true;

            CanUndo = history.Count > 0;
            CanRedo = true;
        }

        public void Redo()
        {
            if (future.Count == 0)
                throw new NothingToRedoException();

            IHistoryAction action = future.Pop();
            history.Push(action);
            Future.RemoveAt(0);
            Past.Add(action);
            acceptNew = false;
            action.Redo();
            acceptNew = true;

            CanUndo = true;
            CanRedo = future.Count > 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Clear()
        {
            Past.Clear();
            Future.Clear();
            history.Clear();
            future.Clear();
            CanUndo = false;
            CanRedo = false;
        }

        protected virtual void OnPropertyChanged([CallerMemberName]
            string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [Serializable]
    public class NothingToRedoException : Exception
    {
    }

    [Serializable]
    public class NothingToUndoException : Exception
    {
    }
}