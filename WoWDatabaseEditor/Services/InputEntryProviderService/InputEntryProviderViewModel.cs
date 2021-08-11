using System;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Services.InputEntryProviderService
{
    public class InputEntryProviderViewModel<T> : ObservableBase, IDialog
    {
        private T entry;

        public InputEntryProviderViewModel(string title, string description, T defaultValue, Func<T, bool>? isValid = null)
        {
            Description = description;
            entry = defaultValue;
            Title = title;
            Save = new DelegateCommand(() =>
            {
                CloseOk?.Invoke();
            }, () => isValid == null || isValid(Entry)).ObservesProperty(() => Entry);
            Cancel = new DelegateCommand(() =>
            {
                CloseCancel?.Invoke();
            });
        }

        public string Description { get; }

        public T Entry
        {
            get => entry;
            set => SetProperty(ref entry, value);
        }

        public DelegateCommand Save { get; }
        public DelegateCommand Cancel { get; }

        public int DesiredWidth { get; } = 460;
        public int DesiredHeight { get; } = 230;
        public string Title { get; }
        public bool Resizeable { get; } = false;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }
}