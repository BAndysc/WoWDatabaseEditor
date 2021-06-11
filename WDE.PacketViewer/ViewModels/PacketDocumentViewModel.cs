using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using DynamicData;
using DynamicData.Binding;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.PacketViewer.Filtering;
using WDE.PacketViewer.PacketParserIntegration;
using WDE.PacketViewer.Processing;
using WDE.PacketViewer.Processing.Processors;
using WDE.PacketViewer.Solutions;
using WoWPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels
{
    [AutoRegister]
    public class PacketDocumentViewModel : ObservableBase, ISolutionItemDocument, IProgress<float>
    {
        private readonly IMainThread mainThread;
        private readonly MostRecentlySearchedService mostRecentlySearchedService;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IGenericTableDocumentService genericTableDocumentService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IPacketFilteringService filteringService;
        private readonly IDbcStore dbcStore;
        private readonly ISpellStore spellStore;
        private readonly IDocumentManager documentManager;
        private readonly ITextDocumentService textDocumentService;
        private readonly ISniffLoader sniffLoader;

        private readonly PacketDocumentSolutionItem solutionItem;
        
        public DelegateCommand RunProcessors { get; }

        public ObservableCollection<ProcessorViewModel> Processors { get; } = new();

        public PacketDocumentViewModel(PacketDocumentSolutionItem solutionItem, 
            IMainThread mainThread,
            MostRecentlySearchedService mostRecentlySearchedService,
            IDatabaseProvider databaseProvider,
            IGenericTableDocumentService genericTableDocumentService,
            Func<INativeTextDocument> nativeTextDocumentCreator,
            IMessageBoxService messageBoxService,
            IPacketFilteringService filteringService,
            IDbcStore dbcStore,
            ISpellStore spellStore,
            IDocumentManager documentManager,
            ITextDocumentService textDocumentService,
            ISniffLoader sniffLoader)
        {
            this.solutionItem = solutionItem;
            this.mainThread = mainThread;
            this.mostRecentlySearchedService = mostRecentlySearchedService;
            this.databaseProvider = databaseProvider;
            this.genericTableDocumentService = genericTableDocumentService;
            this.messageBoxService = messageBoxService;
            this.filteringService = filteringService;
            this.dbcStore = dbcStore;
            this.spellStore = spellStore;
            this.documentManager = documentManager;
            this.textDocumentService = textDocumentService;
            this.sniffLoader = sniffLoader;
            MostRecentlySearched = mostRecentlySearchedService.MostRecentlySearched;
            Title = "Sniff " + Path.GetFileNameWithoutExtension(solutionItem.File);
            SolutionItem = solutionItem;
            FilterText = nativeTextDocumentCreator();
            SelectedPacketPreview = nativeTextDocumentCreator();

            Watch(this, t => t.FilteringProgress, nameof(ProgressUnknown));
            
            filteredPackets = AllPackets;
            ApplyFilterCommand = new AsyncCommand(async () =>
            {
                if (inApplyFilterCommand)
                    return;

                inApplyFilterCommand = true;
                MostRecentlySearchedItem = FilterText.ToString();
                inApplyFilterCommand = false;
                
                if (!string.IsNullOrEmpty(FilterText.ToString()))
                    mostRecentlySearchedService.Add(FilterText.ToString());
                
                filteringToken?.Cancel();
                filteringToken = null;
                await FilterPackets(FilterText.ToString());
            });

            LoadSniff();

            AutoDispose(this.ToObservable(t => t.SelectedPacket).SubscribeAction(doc =>
            {
                if (doc != null)
                {
                    SelectedPacketPreview.FromString(doc.Text);
                }
            }));
            
            Processors.Add(new ProcessorViewModel("Spell names", "Generate all unique spell names as c++ like enum", () => new NameAsEnumProcessor(new SpellNameProcessor(spellStore))));
            Processors.Add(new ProcessorViewModel("Creature/gameobject names", "Generate all unique creature and gameobject names as c++ like enum", () => new NameAsEnumProcessor(new CreatureGameObjectNameProcessor())));
            Processors.Add(new ProcessorViewModel("Story teller", "Presents sniff as human readable story (only some packets)",
                () =>
                {
                    StringBuilder sb = new StringBuilder();
                    StringWriter sw = new StringWriter(sb);
                    return new ToTextProcessor(new StoryTellerProcessor(databaseProvider, dbcStore, spellStore, sw), sb);
                }));

            RunProcessors = new DelegateCommand(() =>
            {
                var procesors = Processors.Where(s => s.IsChecked).ToList();
                if (procesors.Count == 0)
                    return;
                StringBuilder output = new();
                foreach (var selected in procesors)
                {
                    var processor = selected.GetProcessor();
                    foreach (var packet in FilteredPackets)
                    {
                        processor.Process(packet.Packet);
                    }

                    if (procesors.Count > 1)
                        output.Append(" -- " + selected.Name);
                    output.Append(processor.Generate());

                    if (procesors.Count > 1)
                        output.AppendLine("\n\n");
                }

                documentManager.OpenDocument(textDocumentService.CreateDocument(procesors[0].Name, output.ToString()));
            });//, () => Processors.Any(c => c.IsChecked));
            //foreach (var proc in Processors)
            //    RunProcessors.ObservesProperty(() => proc.IsChecked);
        }

        public void Report(float value)
        {
            FilteringProgress = value * 100f;   
        }
        
        private async Task LoadSniff()
        {
            LoadingInProgress = true;
            FilteringInProgress = true;

            try
            {
                var packets = await sniffLoader.LoadSniff(solutionItem.File, this);

                var entryExtractor = new EntryExtractorProcessor();
                using (AllPackets.SuspendNotifications())
                {
                    foreach (var packet in packets.Packets_)
                    {
                        var entry = entryExtractor.Process(packet);
                        AllPackets.Add(new PacketViewModel(packet, packet.BaseData.Time.ToDateTime(), entry));
                    }
                }
            }
            catch (ParserException e)
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Error)
                    .SetTitle("Error with parser")
                    .SetMainInstruction("Parser error")
                    .SetContent(e.Message)
                    .WithOkButton(false)
                    .Build());
                if (CloseCommand != null)
                    await CloseCommand.ExecuteAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            LoadingInProgress = false;
            ApplyFilterCommand.ExecuteAsync();
        }

        private ObservableCollection<PacketViewModel> filteredPackets;
        public ObservableCollection<PacketViewModel> FilteredPackets
        {
            get => filteredPackets;
            set => SetProperty(ref filteredPackets, value);
        }

        public ObservableCollectionExtended<PacketViewModel> AllPackets { get; } = new ObservableCollectionExtended<PacketViewModel>();
        public ReadOnlyObservableCollection<string> MostRecentlySearched { get; }
        
        private string? mostRecentlySearchedItem;
        public string? MostRecentlySearchedItem
        {
            get => mostRecentlySearchedItem;
            set
            {
                if (mostRecentlySearchedItem == value)
                    return;

                if (value != null && !MostRecentlySearched.Contains(value))
                    value = null;
                
                SetProperty(ref mostRecentlySearchedItem, value);
                if (value != null)
                {
                    FilterText.FromString(value);
                    ApplyFilterCommand.ExecuteAsync();   
                }
            }
        }

        private PacketViewModel? selectedPacket;
        public PacketViewModel? SelectedPacket
        {
            get => selectedPacket;
            set => SetProperty(ref selectedPacket, value);
        }

        private float filteringProgress;
        public float FilteringProgress
        {
            get => filteringProgress;
            set => SetProperty(ref filteringProgress, value);
        }

        public bool ProgressUnknown => filteringProgress < 0;
        
        private bool filteringInProgress;
        public bool FilteringInProgress
        {
            get => filteringInProgress;
            set => SetProperty(ref filteringInProgress, value);
        }
        
        private bool loadingInProgress;
        public bool LoadingInProgress
        {
            get => loadingInProgress;
            set => SetProperty(ref loadingInProgress, value);
        }
        
        private bool inApplyFilterCommand = false;
        public AsyncCommand ApplyFilterCommand { get; }

        public INativeTextDocument SelectedPacketPreview { get; }
        public INativeTextDocument FilterText { get; }
        
        private CancellationTokenSource? filteringToken = null;
        private async Task FilterPackets(string filter)
        {
            FilteringInProgress = true;
            var tokenSource = new CancellationTokenSource();
            filteringToken = tokenSource;

            try {
                var result = await filteringService.Filter(AllPackets, filter, tokenSource.Token, this);
                if (result != null)
                    FilteredPackets = result;
            }
            catch (Exception e)
            {
                messageBoxService.ShowDialog<bool>(new MessageBoxFactory<bool>()
                    .SetTitle("Filtering error")
                    .SetMainInstruction("Filtering error")
                    .SetContent(e.Message)
                    .WithOkButton(true)
                    .Build());
            }

            if (filteringToken == tokenSource)
            {
                filteringToken = null;
                FilteringInProgress = false;
            }
        }
        
        public string Title { get; }
        public ImageUri? Icon => new ImageUri("Icons/document_sniff.png");
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save => AlwaysDisabledCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; }
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History => null;
        public ISolutionItem SolutionItem { get; }
    }
}