using System;
using WDE.MVVM;

namespace WDE.DbScriptsEditor.Models
{
    // Shared bulk-edit scaffolding, mirrors EventAiBase's BulkEdit so a single user gesture
    // (e.g. changing a command, which rewrites several columns) is one undo entry.
    public abstract class BulkEditableBase : ObservableBase
    {
        public event Action BulkEditingStarted = delegate { };
        public event Action<string> BulkEditingFinished = delegate { };

        public IDisposable BulkEdit(string name) => new BulkEditing(this, name);

        private class BulkEditing : IDisposable
        {
            private readonly BulkEditableBase owner;
            private readonly string name;

            public BulkEditing(BulkEditableBase owner, string name)
            {
                this.owner = owner;
                this.name = name;
                owner.BulkEditingStarted.Invoke();
            }

            public void Dispose() => owner.BulkEditingFinished.Invoke(name);
        }
    }
}
