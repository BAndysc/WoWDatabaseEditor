using System;
using Prism.Commands;
using WDE.Common.Managers;

namespace WoWDatabaseEditorCore.Services.InputEntryProviderService
{
    public class InputEntryProviderViewModel : IDialog
    {
        public InputEntryProviderViewModel()
        {
            Save = new DelegateCommand(() =>
            {
                CloseOk?.Invoke();
            });
        }

        public uint Entry { get; set; }
        public DelegateCommand Save { get; }

        public int DesiredWidth { get; } = 200;
        public int DesiredHeight { get; } = 150;
        public string Title { get; } = "Enter entry";
        public bool Resizeable { get; } = false;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }
}