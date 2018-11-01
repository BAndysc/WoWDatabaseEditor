
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Events;
using WDE.Common.History;
using Prism.Ioc;
using WDE.Common.Managers;

namespace WDE.HistoryWindow.ViewModels
{
    internal class HistoryEvent
    {
        public string Name { get; set; }
        public bool IsFromFuture { get; set; }
    }

    internal class HistoryViewModel : BindableBase
    {
        public ObservableCollection<HistoryEvent> Items { get; set; } = new ObservableCollection<HistoryEvent>();

        private IDocument previousDocument;

        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public HistoryViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<EventActiveDocumentChanged>().Subscribe(doc =>
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

        private void HistoryCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Items.Clear();
            foreach (var past in previousDocument.History.Past)
            {
                Items.Add(new HistoryEvent() { Name = past.GetDescription(), IsFromFuture = false });
            }

            foreach (var past in previousDocument.History.Future)
            {
                Items.Add(new HistoryEvent() { Name = past.GetDescription(), IsFromFuture = true });
            }
        }
    }
}
