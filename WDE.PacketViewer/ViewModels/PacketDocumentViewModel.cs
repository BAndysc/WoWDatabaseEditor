using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using DynamicData.Binding;
using Prism.Commands;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Disposables;
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
using WDE.PacketViewer.Processing.ProcessorProviders;
using WDE.PacketViewer.Processing.Processors;
using WDE.PacketViewer.Settings;
using WDE.PacketViewer.Solutions;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.ViewModels
{
    [AutoRegister]
    public class PacketDocumentViewModel : ObservableBase, ISolutionItemDocument, IProgress<float>
    {
        private readonly PacketDocumentSolutionItem solutionItem;
        private readonly IMainThread mainThread;
        private readonly IMessageBoxService messageBoxService;
        private readonly IPacketFilteringService filteringService;
        private readonly ISniffLoader sniffLoader;
        private readonly IPacketProcessor<PacketViewModel> packetViewModelCreator;

        public PacketDocumentViewModel(PacketDocumentSolutionItem solutionItem, 
            IMainThread mainThread,
            MostRecentlySearchedService mostRecentlySearchedService,
            IDatabaseProvider databaseProvider,
            Func<INativeTextDocument> nativeTextDocumentCreator,
            IMessageBoxService messageBoxService,
            IPacketFilteringService filteringService,
            IDocumentManager documentManager,
            ITextDocumentService textDocumentService,
            IWindowManager windowManager,
            IPacketViewerSettings packetSettings,
            IEnumerable<IPacketDumperProvider> dumperProviders,
            ISniffLoader sniffLoader)
        {
            this.solutionItem = solutionItem;
            this.mainThread = mainThread;
            this.messageBoxService = messageBoxService;
            this.filteringService = filteringService;
            this.sniffLoader = sniffLoader;
            packetViewModelCreator = new PacketViewModelFactory(databaseProvider);
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

                if (currentActionToken != filteringToken)
                    throw new Exception("Invalid concurrent access!");
                filteringToken?.Cancel();
                filteringToken = null;
                currentActionToken = null;
                await FilterPackets(FilterText.ToString());
            });

            SaveToFileCommand = new AsyncCommand(async () =>
            {
                var path = await windowManager.ShowSaveFileDialog("Text file|txt");
                if (path == null)
                    return;

                LoadingInProgress = true;
                FilteringInProgress = true;
                try
                {
                    await using StreamWriter writer = File.CreateText(path);
                    int i = 0;
                    int count = FilteredPackets.Count;
                    foreach (var packet in FilteredPackets)
                    {
                        await writer.WriteLineAsync(packet.Text);
                        if ((i % 100) == 0)
                            Report(i * 1.0f / count);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await this.messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Fatal error")
                        .SetMainInstruction("Fatal error during saving file")
                        .SetContent(e.Message)
                        .WithOkButton(false)
                        .Build());
                }
                LoadingInProgress = false;
                FilteringInProgress = false;
            });
            
            OpenHelpCommand = new DelegateCommand(() => windowManager.OpenUrl("https://github.com/BAndysc/WoWDatabaseEditor/wiki/Packet-Viewer"));
            
            On(() => SelectedPacket, doc =>
            {
                if (doc != null)
                    SelectedPacketPreview.FromString(doc.Text);
            });

            On(() => SplitUpdate, _ =>
            {
                SplitPacketsIfNeededAsync().Wait();
                ((ICommand)ApplyFilterCommand).Execute(null);
            });
            
            foreach (var dumper in dumperProviders)
                Processors.Add(new(dumper));

            RunProcessors = new AsyncAutoCommand(async () =>
            {
                var processors = Processors.Where(s => s.IsChecked).ToList();
                if (processors.Count == 0)
                    return;

                LoadingInProgress = true;
                FilteringInProgress = true;
                try
                {
                    var tokenSource = new CancellationTokenSource();
                    AssertNoOnGoingTask();
                    currentActionToken = tokenSource;

                    var output = await RunProcessorsThreaded(processors, tokenSource.Token).ConfigureAwait(true);

                    if (!tokenSource.IsCancellationRequested)
                    {
                        var extension = processors.Select(p => p.Extension).Distinct().Count() == 1
                            ? processors[0].Extension
                            : "txt";
                        documentManager.OpenDocument(textDocumentService.CreateDocument( $"{processors[0].Name} ({Title})", output,  extension));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetIcon(MessageBoxIcon.Error)
                        .SetTitle("Fatal error")
                        .SetMainInstruction("Fatal error during processing")
                        .SetContent(
                            "Sorry, fatal error occured, this is probably a bug in processors, please report it in github\n\n" + e)
                        .WithOkButton(true)
                        .Build());
                }
                LoadingInProgress = false;
                FilteringInProgress = false;
                currentActionToken = null;

            }, _ => Processors.Any(c => c.IsChecked));
            
            foreach (var proc in Processors)
                AutoDispose(proc.ToObservable(p => p.IsChecked)
                    .SubscribeAction(_ => RunProcessors.RaiseCanExecuteChanged()));

            wrapLines = packetSettings.Settings.WrapLines;
            splitUpdate = packetSettings.Settings.AlwaysSplitUpdates;
            if (!string.IsNullOrEmpty(packetSettings.Settings.DefaultFilter))
                FilterText.FromString(packetSettings.Settings.DefaultFilter);
            LoadSniff().ListenErrors();

            AutoDispose(new ActionDisposable(() => currentActionToken?.Cancel()));
        }

        private Task SplitPacketsIfNeededAsync()
        {
            if (!splitUpdate)
                return Task.CompletedTask;
            
            if (AllPacketsSplit != null) 
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                AllPacketsSplit = new();
                var splitter = new SplitUpdateProcessor();
                foreach (var packet in AllPackets)
                {
                    var splitted = splitter.Process(packet.Packet);
                    if (splitted == null)
                        AllPacketsSplit.Add(packet);
                    else
                    {
                        foreach (var split in splitted)
                            AllPacketsSplit.Add(packetViewModelCreator.Process(split)!);
                    }
                }
            });
        }

        public async Task<string> RunProcessorsThreaded(IList<ProcessorViewModel> processors, CancellationToken cancellationToken)
        {
            List<(ProcessorViewModel viewModel, IPacketTextDumper dumper)> dumpers = new();
            foreach (var processor in processors)
                dumpers.Add((processor, await processor.CreateProcessor()));
            
            return await Task.Run(() =>
            {
                StringBuilder output = new();
                var packetsCount = (long)FilteredPackets.Count * processors.Count;
                long i = 0;
                foreach (var pair in dumpers)
                {
                    foreach (var packet in FilteredPackets)
                    {
                        i++;
                        pair.dumper.Process(packet.Packet);
                        if ((i % 100) == 0)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;
                            Report(i * 1.0f / packetsCount);
                        }
                    }

                    if (processors.Count > 1)
                        output.AppendLine(" -- " + pair.viewModel.Name);
                    output.Append(pair.dumper.Generate());

                    if (processors.Count > 1)
                        output.AppendLine("\n\n");
                }

                return output.ToString();
            }, cancellationToken).ConfigureAwait(true);
        }

        public void Report(float value)
        {
            mainThread.Dispatch(() => FilteringProgress = value * 100f);
        }
        
        private async Task LoadSniff()
        {
            LoadingInProgress = true;
            FilteringInProgress = true;

            AssertNoOnGoingTask();
            currentActionToken = new CancellationTokenSource();
            
            try
            {
                var packets = await sniffLoader.LoadSniff(solutionItem.File, currentActionToken.Token, this);

                if (currentActionToken.IsCancellationRequested)
                {
                    LoadingInProgress = false;
                    currentActionToken = null;
                    return;
                }
                
                using (AllPackets.SuspendNotifications())
                {
                    foreach (var packet in packets.Packets_)
                        AllPackets.Add(packetViewModelCreator.Process(packet)!);
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

            FilteringProgress = -1;
            await SplitPacketsIfNeededAsync().ConfigureAwait(true);
            LoadingInProgress = false;
            FilteringInProgress = false;
            currentActionToken = null;
            await ApplyFilterCommand.ExecuteAsync();
        }

        private void AssertNoOnGoingTask()
        {
            if (currentActionToken != null)
            {
                Console.WriteLine("Invalid concurrent task access!");
                throw new Exception("Invalid concurrent task access!");
            }
        }
        
        private async Task FilterPackets(string filter)
        {
            FilteringInProgress = true;
            var tokenSource = new CancellationTokenSource();
            filteringToken = tokenSource;    
            AssertNoOnGoingTask();
            currentActionToken = tokenSource;

            try {
                var result = await filteringService.Filter(splitUpdate && AllPacketsSplit != null ? AllPacketsSplit : AllPackets, filter, tokenSource.Token, this);
                if (result != null)
                    FilteredPackets = result;
            }
            catch (Exception e)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Filtering error")
                    .SetMainInstruction("Filtering error")
                    .SetContent(e.Message)
                    .WithOkButton(true)
                    .Build());
            }

            if (filteringToken == tokenSource)
            {
                currentActionToken = null;
                filteringToken = null;
                FilteringInProgress = false;
            }
        }

        #region Properties
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

        private ObservableCollection<PacketViewModel> filteredPackets;
        public ObservableCollection<PacketViewModel> FilteredPackets
        {
            get => filteredPackets;
            set => SetProperty(ref filteredPackets, value);
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
        
        private bool splitUpdate;
        public bool SplitUpdate
        {
            get => splitUpdate;
            set => SetProperty(ref splitUpdate, value);
        }
        
        private bool wrapLines;
        public bool WrapLines
        {
            get => wrapLines;
            set => SetProperty(ref wrapLines, value);
        }
        #endregion

        private bool inApplyFilterCommand;
        private CancellationTokenSource? filteringToken;
        private CancellationTokenSource? currentActionToken;
        public AsyncAutoCommand RunProcessors { get; }
        public ObservableCollection<ProcessorViewModel> Processors { get; } = new();
        private ObservableCollectionExtended<PacketViewModel> AllPackets { get; } = new ();
        private ObservableCollectionExtended<PacketViewModel>? AllPacketsSplit { get; set; }
        public ReadOnlyObservableCollection<string> MostRecentlySearched { get; }

        public ICommand OpenHelpCommand { get; }
        public IAsyncCommand SaveToFileCommand { get; }
        public AsyncCommand ApplyFilterCommand { get; }
        public INativeTextDocument SelectedPacketPreview { get; }
        public INativeTextDocument FilterText { get; }
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
        public bool ShowExportToolbarButtons => false;
    }
}