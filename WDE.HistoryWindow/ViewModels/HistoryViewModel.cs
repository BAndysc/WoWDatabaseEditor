using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WDE.HistoryWindow.ViewModels
{
    internal class HistoryEvent
    {
        public string Name { get; set; }
        public bool IsFromFuture { get; set; }
    }

    [AutoRegister]
    [SingleInstance]
    internal class HistoryViewModel : BindableBase, ITool
    {
        private Visibility visibility;

        private IDocument previousDocument;

        public string UniqueId => "history_view";
        public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Right;
        public bool OpenOnStart => true;

        public HistoryViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<EventActiveDocumentChanged>()
                .Subscribe(doc =>
                {
                    Items.Clear();

                    if (previousDocument != null)
                    {
                        previousDocument.History.Past.CollectionChanged -= HistoryCollectionChanged;
                        previousDocument.History.Future.CollectionChanged -= HistoryCollectionChanged;
                        previousDocument = null;
                    }

                    if (doc == null || doc.History == null)
                        return;

                    previousDocument = doc;

                    doc.History.Past.CollectionChanged += HistoryCollectionChanged;
                    doc.History.Future.CollectionChanged += HistoryCollectionChanged;

                    HistoryCollectionChanged(null, null);
                });
        }

        public ObservableCollection<HistoryEvent> Items { get; set; } = new();

        public string Title => "History";

        public Visibility Visibility
        {
            get => visibility;
            set => SetProperty(ref visibility, value);
        }

        private void HistoryCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Items.Clear();
            foreach (IHistoryAction past in previousDocument.History.Past)
                Items.Add(new HistoryEvent {Name = past.GetDescription(), IsFromFuture = false});

            foreach (IHistoryAction past in previousDocument.History.Future)
                Items.Add(new HistoryEvent {Name = past.GetDescription(), IsFromFuture = true});
        }
    }
}