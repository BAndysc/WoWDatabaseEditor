using System.Reactive.Linq;
using Dock.Model.Controls;
using Dock.Model.ReactiveUI.Controls;
using WDE.Common.Managers;
using WDE.MVVM;
using WDE.MVVM.Observable;
using IDocument = WDE.Common.Managers.IDocument;

namespace WoWDatabaseEditorCore.Avalonia.Docking
{
    public class AvaloniaDocumentDockWrapper : Document, IDockableFocusable
    {
        private readonly IDocumentManager documentManager;
        public IDocument ViewModel { get; }
        public bool CanReallyClose { get; set; }

        private static int uniqueId;
        
        public AvaloniaDocumentDockWrapper(IDocumentManager documentManager, IDocument document)
        {
            this.documentManager = documentManager;
            Id = $"{(uniqueId++)}";
            Title = document.Title;
            ViewModel = document;
            CanClose = true;
            CanFloat = false;
            CanPin = false;
            
            var title = document.ToObservable(d => d.Title);
            var isModified = document.ToObservable(d => d.IsModified);
            
            title.CombineLatest(isModified).SubscribeAction(a => { Title = a.Second ? $"{a.First} *" : $"{a.First}"; });
        }

        public override void OnSelected()
        {
            documentManager.ActiveDocument = ViewModel;
        }

        public override bool OnClose()
        {
            if (CanReallyClose)
                return true;
            
            if (!ViewModel.CanClose)
                return false;

            ViewModel.CloseCommand.Execute(null);
            return false;
        }

        public void OnFocus()
        {
            documentManager.ActiveDocument = ViewModel;
        }
    }
}