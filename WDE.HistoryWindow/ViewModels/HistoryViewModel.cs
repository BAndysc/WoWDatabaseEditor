
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

namespace WDE.HistoryWindow.ViewModels
{
    internal class Valuea
    {
        public string Name { get; set; }
        public bool Aaaa { get; set; }
    }

    internal class HistoryViewModel : BindableBase
    {
        public ObservableCollection<Valuea> Items { get; set; } = new ObservableCollection<Valuea>();

        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        ~HistoryViewModel()
        {

        }

        public HistoryViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<EventActiveDocumentChanged>().Subscribe(doc =>
            {
                if (doc.History == null)
                    return;

                doc.History.Past.CollectionChanged += (sender, e) =>
                {
                    Items.Clear();
                    foreach (var past in doc.History.Past)
                    {
                        Items.Add(new Valuea() { Name = past.GetDescription(), Aaaa = false });
                    }

                    foreach (var past in doc.History.Future)
                    {
                        Items.Add(new Valuea() { Name = past.GetDescription(), Aaaa = true });
                    }
                };
                doc.History.Future.CollectionChanged += (sender, e) =>
                {
                    Items.Clear();
                    foreach (var past in doc.History.Past)
                    {
                        Items.Add(new Valuea() { Name = past.GetDescription(), Aaaa = false });
                    }

                    foreach (var past in doc.History.Future)
                    {
                        Items.Add(new Valuea() { Name = past.GetDescription(), Aaaa = true });
                    }
                };

                Items.Clear();
                foreach (var past in doc.History.Past)
                {
                    Items.Add(new Valuea() { Name = past.GetDescription(), Aaaa = false });
                }

                foreach (var past in doc.History.Future)
                {
                    Items.Add(new Valuea() { Name = past.GetDescription(), Aaaa = true });
                }
            });
        }
    }
}
