using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Events;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Services.ProblemsTool
{
    [SingleInstance]
    [AutoRegister]
    public class ProblemsViewModel : ObservableBase, ITool
    {
        private readonly Lazy<IDocumentManager> documentManager;
        private Dictionary<IDocument, DocumentProblemsViewModel> DocumentToViewModel { get; } = new();
        public ObservableCollection<DocumentProblemsViewModel> Problems { get; } = new();
        
        public ProblemsViewModel(Lazy<IDocumentManager> documentManager, IEventAggregator eventAggregator)
        {
            this.documentManager = documentManager;

            AutoDispose(eventAggregator.GetEvent<AllModulesLoaded>().Subscribe(Bind, true));
        }

        public event Action<DocumentProblemsViewModel>? RequestScrollTo;
        
        public string Title => "Problems";
        public string UniqueId => "problems";

        private bool visibility;
        public bool Visibility
        {
            get => visibility;
            set => SetProperty(ref visibility, value);
        }

        public int TotalProblems => Problems.Select(p => p.Count).Sum();
        
        private void Bind()
        {
            documentManager.Value.ToObservable(d => d.ActiveDocument).SubscribeAction(active =>
            {
                if (active == null)
                    return;

                if (!DocumentToViewModel.TryGetValue(active, out var activeProblems))
                    return;

                if (!activeProblems.Added)
                    return;

                RequestScrollTo?.Invoke(activeProblems);
            });

            documentManager.Value.OpenedDocuments.ToStream().SubscribeAction(elem =>
            {
                if (elem.Item is not IProblemSourceDocument problemSource)
                    return;

                if (elem.Type == CollectionEventType.Add)
                {
                    var problemsViewModel = new DocumentProblemsViewModel(problemSource);
                    DocumentToViewModel[elem.Item] = problemsViewModel;
                    problemsViewModel.Subscription = problemSource.Problems.SubscribeAction(newProblems =>
                    {
                        if (newProblems.Count > 0 && !problemsViewModel.Added)
                        {
                            problemsViewModel.Added = true;
                            Problems.Add(problemsViewModel);
                        }
                        else if (newProblems.Count == 0 && problemsViewModel.Added)
                        {
                            problemsViewModel.Added = false;
                            Problems.Remove(problemsViewModel);
                        }

                        problemsViewModel.Clear();
                        foreach (var problem in newProblems)
                        {
                            problemsViewModel.Add(new ProblemViewModel(problem));
                        }
                        
                        RaisePropertyChanged(nameof(TotalProblems));
                    });
                    RaisePropertyChanged(nameof(TotalProblems));
                }
                else
                {
                    var vm = DocumentToViewModel[elem.Item];
                    vm.Subscription?.Dispose();
                    if (vm.Added)
                        Problems.Remove(vm);
                    DocumentToViewModel.Remove(elem.Item);
                    RaisePropertyChanged(nameof(TotalProblems));
                }
            });
        }

        public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Bottom;
        public bool OpenOnStart => true;
        public bool IsSelected { get; set; }
    }
}