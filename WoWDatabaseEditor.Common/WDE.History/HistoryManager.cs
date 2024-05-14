using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.History
{
    [AutoRegister]
    public class HistoryManager : IHistoryManager, INotifyPropertyChanged
    {
        private IHistoryAction? savedPoint;
        private bool forceNoSave;
        private bool acceptNew;
        private bool canRedo;
        private bool canUndo;
        private bool isSaved;
        private int? limit;

        public HistoryManager()
        {
            savedPoint = null;
            acceptNew = true;
            forceNoSave = false;
            RecalculateValues();
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

        public bool IsSaved
        {
            get => isSaved;
            private set
            {
                isSaved = value;
                OnPropertyChanged();
            }
        }

        public bool IsUndoing => !acceptNew;
        public int UndoCount => Past.Count;
        public int RedoCount => Future.Count;
        public event Action? OnUndo;
        public event Action? OnRedo;

        public void RemoveHandler(HistoryHandler handler)
        {
            handler.ActionPush -= HandlerOnActionPush;
            handler.ActionDone -= HandlerOnActionDone;
            handler.ActionDoneWithoutHistory -= HandlerOnActionDoneWithoutHistory;
        }

        public T AddHandler<T>(T handler) where T : HistoryHandler
        {
            handler.ActionPush += HandlerOnActionPush;
            handler.ActionDone += HandlerOnActionDone;
            handler.ActionDoneWithoutHistory += HandlerOnActionDoneWithoutHistory;
            return handler;
        }

        private void HandlerOnActionDoneWithoutHistory(object? sender, IHistoryAction action)
        {
            if (!acceptNew)
                throw new Exception("Cannot do history action when not accepting new actions");

            try
            {
                acceptNew = false;
                action.Redo();
            }
            finally
            {
                acceptNew = true;
            }
        }

        private void HandlerOnActionDone(object? sender, IHistoryAction action)
        {
            if (!acceptNew)
                throw new Exception("Cannot do history action when not accepting new actions");

            try
            {
                acceptNew = false;

                EnsureLimits();
                Past.Add(action);
                Future.Clear();
                RecalculateValues();

                action.Redo();
                OnRedo?.Invoke();
            }
            finally
            {
                acceptNew = true;
            }
        }

        private void HandlerOnActionPush(object? sender, IHistoryAction e)
        {
            if (!acceptNew)
                return;

            EnsureLimits();
            Past.Add(e);
            Future.Clear();
            RecalculateValues();
        }

        private void EnsureLimits()
        {
            if (limit.HasValue && Past.Count == limit.Value)
                Past.RemoveAt(0);
        }

        public void Undo()
        {
            Profiler.Start();
            if (Past.Count == 0)
                throw new NothingToUndoException();

            IHistoryAction action = Past[^1];
            Past.RemoveAt(Past.Count - 1);
            Future.Insert(0, action);
            acceptNew = false;
            action.Undo();
            acceptNew = true;

            RecalculateValues();

            OnUndo?.Invoke();
            Profiler.Stop();
        }

        public void Redo()
        {
            if (Future.Count == 0)
                throw new NothingToRedoException();

            IHistoryAction action = Future[0];
            Future.RemoveAt(0);
            Past.Add(action);
            acceptNew = false;
            action.Redo();
            acceptNew = true;

            RecalculateValues();

            OnRedo?.Invoke();
        }

        public void MarkAsSaved()
        {
            if (Past.Count > 0)
                savedPoint = Past[^1];
            else
                savedPoint = null;
            forceNoSave = false;
            RecalculateValues();
        }

        public void MarkNoSave()
        {
            savedPoint = null;
            forceNoSave = true;
            RecalculateValues();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Clear()
        {
            Past.Clear();
            Future.Clear();
            CanUndo = false;
            CanRedo = false;
            IsSaved = true;
        }

        public void LimitStack(int newLimit)
        {
            this.limit = newLimit;
        }

        private void RecalculateValues()
        {
            CanUndo = Past.Count > 0;
            CanRedo = Future.Count > 0;
            IsSaved = !forceNoSave && ((Past.Count == 0 && savedPoint == null) || (Past.Count > 0 && savedPoint == Past[^1]));
        }

        protected virtual void OnPropertyChanged([CallerMemberName]
            string? propertyName = null)
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