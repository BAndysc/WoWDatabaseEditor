using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.History;

namespace WDE.History
{
    [WDE.Module.Attributes.AutoRegister]
    public class HistoryManager : IHistoryManager, INotifyPropertyChanged
    {
        private readonly Stack<IHistoryAction> _history;
        private readonly Stack<IHistoryAction> _future;

        public ObservableCollection<IHistoryAction> Past { get; } = new ObservableCollection<IHistoryAction>();
        public ObservableCollection<IHistoryAction> Future { get; } = new ObservableCollection<IHistoryAction>();

        public bool CanUndo
        {
            get { return _canUndo; }
            private set
            {
                _canUndo = value;
                OnPropertyChanged();
            }
        }

        public bool CanRedo
        {
            get { return _canRedo; }
            private set
            {
                _canRedo = value;
                OnPropertyChanged();
            }
        }

        public int UndoCount => _history.Count;
        public int RedoCount => _future.Count;


        private bool _acceptNew;
        private bool _canUndo;
        private bool _canRedo;

        public HistoryManager()
        {
            _history = new Stack<IHistoryAction>();
            _future = new Stack<IHistoryAction>();
            CanUndo = false;
            CanRedo = false;
            _acceptNew = true;
        }

        public void AddHandler(HistoryHandler handler)
        {
            handler.ActionPush += (sender, action) => {
                if (!_acceptNew)
                    return;

                _history.Push(action);
                Past.Add(action);
                _future.Clear();
                Future.Clear();
                CanRedo = false;
                CanUndo = true;
            };
        }

        public void Undo()
        {
            if (_history.Count == 0)
                throw new NothingToUndoException();

            IHistoryAction action = _history.Pop();
            _future.Push(action);
            Past.RemoveAt(Past.Count - 1);
            Future.Insert(0, action);
            _acceptNew = false;
            action.Undo();
            _acceptNew = true;

            CanUndo = _history.Count > 0;
            CanRedo = true;
        }

        public void Clear()
        {
            Past.Clear();
            Future.Clear();
            _history.Clear();
            _future.Clear();
            CanUndo = false;
            CanRedo = false;
        }

        public void Redo()
        {
            if (_future.Count == 0)
                throw new NothingToRedoException();

            IHistoryAction action = _future.Pop();
            _history.Push(action);
            Future.RemoveAt(0);
            Past.Add(action);
            _acceptNew = false;
            action.Redo();
            _acceptNew = true;

            CanUndo = true;
            CanRedo = _future.Count > 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [Serializable]
    public class NothingToRedoException : Exception
    {
        public NothingToRedoException()
        {
        }
    }

    [Serializable]
    public class NothingToUndoException : Exception
    {
        public NothingToUndoException()
        {
        }
    }
}
