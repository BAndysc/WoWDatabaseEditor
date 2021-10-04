using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using DynamicData;
using DynamicData.Binding;
using Prism.Commands;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Disposables;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Providers;
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
using WDE.PacketViewer.Processing.Processors.ActionReaction;
using WDE.PacketViewer.Services;
using WDE.PacketViewer.Settings;
using WDE.PacketViewer.Solutions;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
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
        private readonly IActionReactionProcessorCreator actionReactionProcessorCreator;
        private readonly IRelatedPacketsFinder relatedPacketsFinder;
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
            IInputBoxService inputBoxService,
            IHistoryManager history,
            IPacketFilterDialogService filterDialogService,
            IActionReactionProcessorCreator actionReactionProcessorCreator,
            IRelatedPacketsFinder relatedPacketsFinder,
            ITeachingTipService teachingTipService,
            PacketDocumentSolutionNameProvider solutionNameProvider,
            ISniffLoader sniffLoader,
            ISpellStore spellStore)
        {
            this.solutionItem = solutionItem;
            this.mainThread = mainThread;
            this.messageBoxService = messageBoxService;
            this.filteringService = filteringService;
            this.actionReactionProcessorCreator = actionReactionProcessorCreator;
            this.relatedPacketsFinder = relatedPacketsFinder;
            this.sniffLoader = sniffLoader;
            History = history;
            history.LimitStack(20);
            packetViewModelCreator = new PacketViewModelFactory(databaseProvider, spellStore);
            MostRecentlySearched = mostRecentlySearchedService.MostRecentlySearched;
            Title = solutionNameProvider.GetName(this.solutionItem);
            SolutionItem = solutionItem;
            FilterText = nativeTextDocumentCreator();
            SelectedPacketPreview = nativeTextDocumentCreator();
            SelectedPacketPreview.DisableUndo();
            Watch(this, t => t.FilteringProgress, nameof(ProgressUnknown));

            AutoDispose(history.AddHandler(new SelectedPacketHistory(this)));
            
            filteredPackets = visiblePackets = AllPackets;
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
            
            On(() => HidePlayerMove, _ =>
            {
                ((ICommand)ApplyFilterCommand).Execute(null);
            });
            
            On(() => DisableFilters, _ =>
            {
                ((ICommand)ApplyFilterCommand).Execute(null);
            });
            
            foreach (var dumper in dumperProviders)
                Processors.Add(new(dumper));

            QuickRunProcessor = new AsyncAutoCommand<ProcessorViewModel>(async (processor) =>
            {
                LoadingInProgress = true;
                FilteringInProgress = true;
                try
                {
                    var tokenSource = new CancellationTokenSource();
                    AssertNoOnGoingTask();
                    currentActionToken = tokenSource;

                    var output = await RunProcessorsThreaded(new List<ProcessorViewModel>(){processor}, tokenSource.Token).ConfigureAwait(true);

                    if (output != null && !tokenSource.IsCancellationRequested)
                        documentManager.OpenDocument(textDocumentService.CreateDocument( $"{processor.Name} ({Title})", output,  processor.Extension, true));
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
            });
            
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

                    if (output != null && !tokenSource.IsCancellationRequested)
                    {
                        var extension = processors.Select(p => p.Extension).Distinct().Count() == 1
                            ? processors[0].Extension
                            : "txt";
                        documentManager.OpenDocument(textDocumentService.CreateDocument( $"{processors[0].Name} ({Title})", output,  extension, true));
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

            UndoCommand = new DelegateCommand(history.Undo, () => history.CanUndo);
            RedoCommand = new DelegateCommand(history.Redo, () => history.CanRedo);
            History.PropertyChanged += (_, _) =>
            {
                UndoCommand.RaiseCanExecuteChanged();
                RedoCommand.RaiseCanExecuteChanged();
            };

            IEnumerable<int> GetFindEnumerator(int start, int count, int direction, bool wrap)
            {
                for (int i = start + direction; i >= 0 && i < count; i += direction)
                    yield return i;
                
                if (wrap)
                {
                    if (direction > 0)
                    {
                        for (int i = 0; i < start; ++i)
                            yield return i;
                    }
                    else
                    {
                        for (int i = count - 1; i > start; --i)
                            yield return i;
                    }
                }
            }
            
            async Task Find(string searchText, int start, int direction)
            {
                var count = VisiblePackets.Count;
                var searchToLower = searchText.ToLower();
                foreach (var i in GetFindEnumerator(start, count, direction, true))
                {
                    if (VisiblePackets[i].Text.Contains(searchToLower, StringComparison.InvariantCultureIgnoreCase))
                    {
                        SelectedPacket = VisiblePackets[i];
                        return;
                    }   
                }
                
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Find")
                    .SetMainInstruction("Not found")
                    .SetContent("Cannot find text: " + searchText)
                    .WithOkButton(true)
                    .Build());
            }

            ToggleFindCommand = new DelegateCommand(() => FindPanelEnabled = !FindPanelEnabled);
            CloseFindCommand = new DelegateCommand(() => FindPanelEnabled = false);
            FindPreviousCommand = new AsyncAutoCommand<string>(async searchText =>
            {
                var start = SelectedPacket != null ? VisiblePackets.IndexOf(SelectedPacket) : 0;
                await Find(searchText, start, -1);
            }, str => str is string s && !string.IsNullOrEmpty(s));
            FindNextCommand = new AsyncAutoCommand<string>(async searchText =>
            {
                var start = SelectedPacket != null ? VisiblePackets.IndexOf(SelectedPacket) : 0;
                await Find(searchText, start, 1);
            }, str => str is string s && !string.IsNullOrEmpty(s));

            FindRelatedPacketsCommands = new AsyncAutoCommand(async () =>
            {
                if (selectedPacket == null)
                    return;

                if (!await EnsureSplitOrDismiss())
                    return;

                FilteringInProgress = true;
                FilteringProgress = -1;
                var tokenSource = new CancellationTokenSource();
                AssertNoOnGoingTask();
                currentActionToken = tokenSource;
                IFilterData? newFilterData = await GetRelatedFilters(selectedPacket.Packet.BaseData.Number, tokenSource.Token);
                FilteringInProgress = false;
                currentActionToken = null;

                if (newFilterData == null)
                    return;

                if (!FilterData.IsEmpty)
                {
                    if (await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Find related packets")
                        .SetMainInstruction("Your current filter data is not empty")
                        .SetContent("Do you want to override current filter data?")
                        .WithButton("Override", true)
                        .WithButton("Merge", false)
                        .Build()))
                    {
                        FilterData = newFilterData;
                    }
                    else
                    {
                        FilterData.SetMinMax(Min(FilterData.MinPacketNumber, newFilterData.MinPacketNumber),
                            Max(FilterData.MaxPacketNumber, newFilterData.MaxPacketNumber));
                        if (newFilterData.IncludedGuids != null)
                            foreach (var guid in newFilterData.IncludedGuids)
                                FilterData.IncludeGuid(guid);
                        if (newFilterData.ForceIncludePacketNumbers != null)
                            foreach (var packet in newFilterData.ForceIncludePacketNumbers)
                                FilterData.IncludePacketNumber(packet);
                    }
                }
                else
                    FilterData = newFilterData;

                await ApplyFilterCommand.ExecuteAsync();
                
            });

            ExcludeEntryCommand = new AsyncAutoCommand<PacketViewModel?>(async vm =>
            {
                if (vm == null || vm.Entry == 0)
                    return;
                
                FilterData.ExcludeEntry(vm.Entry);
                await ApplyFilterCommand.ExecuteAsync();
            }, vm => vm is PacketViewModel pvm && pvm.Entry != 0);

            IncludeEntryCommand = new AsyncAutoCommand<PacketViewModel?>(async vm =>
            {
                if (vm == null || vm.Entry == 0)
                    return;
                
                FilterData.IncludeEntry(vm.Entry);
                await ApplyFilterCommand.ExecuteAsync();
            }, vm => vm is PacketViewModel pvm && pvm.Entry != 0);
            
            ExcludeGuidCommand = new AsyncAutoCommand<PacketViewModel?>(async vm =>
            {
                if (vm == null || vm.MainActor == null)
                    return;
                
                FilterData.ExcludeGuid(vm.MainActor);
                await ApplyFilterCommand.ExecuteAsync();
            }, vm => vm is PacketViewModel pvm && pvm.MainActor != null);
            
            IncludeGuidCommand = new AsyncAutoCommand<PacketViewModel?>(async vm =>
            {
                if (vm == null || vm.MainActor == null)
                    return;
                
                FilterData.IncludeGuid(vm.MainActor);
                await ApplyFilterCommand.ExecuteAsync();
            }, vm => vm is PacketViewModel pvm && pvm.MainActor != null);
            
            ExcludeOpcodeCommand = new AsyncAutoCommand<PacketViewModel?>(async vm =>
            {
                if (vm == null)
                    return;
                
                FilterData.ExcludeOpcode(vm.Opcode);
                await ApplyFilterCommand.ExecuteAsync();
            },  vm => vm != null);
            
            IncludeOpcodeCommand = new AsyncAutoCommand<PacketViewModel?>(async vm =>
            {
                if (vm == null)
                    return;
                
                FilterData.IncludeOpcode(vm.Opcode);
                await ApplyFilterCommand.ExecuteAsync();
            }, vm => vm != null);
            
            AutoDispose(this.ToObservable(o => o.SelectedPacket).SubscribeAction(_ =>
            {
                ExcludeEntryCommand.RaiseCanExecuteChanged();
                IncludeEntryCommand.RaiseCanExecuteChanged();
                ExcludeGuidCommand.RaiseCanExecuteChanged();
                IncludeGuidCommand.RaiseCanExecuteChanged();
                ExcludeOpcodeCommand.RaiseCanExecuteChanged();
                IncludeOpcodeCommand.RaiseCanExecuteChanged();
            }));

            ExplainSelectedPacketCommand = new AsyncAutoCommand(async () =>
            {
                if (!reasonPanelVisibility || actionReactionProcessor == null)
                    return;
                if (selectedPacket == null)
                    return;
                
                DetectedActions.Clear();
                var detected = actionReactionProcessor.GetAllActions(selectedPacket.Id);
                if (detected != null)
                    DetectedActions.AddRange(detected.Select(s => new DetectedActionViewModel(s)));
                
                DetectedEvents.Clear();
                var events = actionReactionProcessor.GetAllEvents(selectedPacket.Id);
                if (events != null)
                    DetectedEvents.AddRange(events.Select(s => new DetectedEventViewModel(s)));
                
                var reasons = actionReactionProcessor.GetPossibleEventsForAction(selectedPacket.Packet.BaseData);
                Predictions.Clear();
                foreach (var reason in reasons)
                {
                    Predictions.Add(new ActionReasonPredictionViewModel(selectedPacket.Packet.BaseData, reason.rate.Value, reason.rate.Explain, reason.@event));
                }
                PossibleActions.Clear();
                var actions = actionReactionProcessor.GetPossibleActionsForEvent(selectedPacket.Packet.BaseData.Number);
                foreach (var action in actions)
                {
                    var actionHappened = actionReactionProcessor.GetAction(action.packetId);
                    if (actionHappened == null)
                        continue;
                    PossibleActions.Add(new PossibleActionViewModel(selectedPacket.Packet.BaseData, action.chance, "", actionHappened.Value));
                }
            }, _ => ReasonPanelVisibility);

            On(() => ReasonPanelVisibility, _ =>
            {
                ApplyFilterCommand.ExecuteAsync().ContinueWith(_ =>
                {
                    ExplainSelectedPacketCommand.Execute(null);
                }, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted).ListenErrors();
            });

            AutoDispose(this.ToObservable(o => o.SelectedPacket)
                .CombineLatest(this.ToObservable(o => o.ReasonPanelVisibility))
                .SubscribeAction(
                _ =>
                {
                    ExplainSelectedPacketCommand.Execute(null);
                }));
            
            OpenFilterDialogCommand = new AsyncAutoCommand(async () =>
            {
                var newData = await filterDialogService.OpenFilterDialog(FilterData);
                if (newData != null)
                {
                    FilterData = newData;
                    await ApplyFilterCommand.ExecuteAsync();
                }
            });

            JumpToPacketCommand = new DelegateCommand<int?>(packetId =>
            {
                if (!packetId.HasValue)
                    return;
                var packet = filteredPackets.FirstOrDefault(p => p.Id == packetId.Value);
                if (packet != null)
                    SelectedPacket = packet;
            });
            
            GoToPacketCommand = new AsyncAutoCommand(async () =>
            {
                var min = filteredPackets[0].Id;
                var max = filteredPackets[^1].Id;
                var jumpTo = await inputBoxService.GetUInt("Go to packet number", $"Specify the packet id to jump to ({min}-{max})");
                if (!jumpTo.HasValue)
                    return;

                int? jumpToId = null;
                for (var index = 0; index < filteredPackets.Count; index++)
                {
                    if (filteredPackets[index].Id == jumpTo.Value)
                    {
                        jumpToId = index;
                        break;
                    }
                }

                if (jumpToId.HasValue)
                    SelectedPacket = filteredPackets[jumpToId.Value];
            }, _ => filteredPackets.Count > 0);

            AutoDispose(this.ToObservable(o => o.FilteredPackets)
                .SubscribeAction(_ => GoToPacketCommand.RaiseCanExecuteChanged()));
            
            wrapLines = packetSettings.Settings.WrapLines;
            splitUpdate = packetSettings.Settings.AlwaysSplitUpdates;
            hidePlayerMove = packetSettings.Settings.AlwaysHidePlayerMovePackets;
            if (!string.IsNullOrEmpty(packetSettings.Settings.DefaultFilter))
                FilterText.FromString(packetSettings.Settings.DefaultFilter);

            On(() => SplitUpdate, @is =>
            {
                if (!@is)
                    return;
                if (teachingTipService.ShowTip("PACKETS_SPLIT_UPDATE"))
                    ShowSplitUpdateTip = true;
            });

            LoadSniff().ListenErrors();

            AutoDispose(new ActionDisposable(() => currentActionToken?.Cancel()));
        }

        private async Task<bool> EnsureSplitOrDismiss()
        {
            if (!splitUpdate)
            {
                if (!await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Packet processor")
                    .SetMainInstruction("Please enable Split UPDATE_OBJECT")
                    .SetContent(
                        "In order to make this package processor work, you must enable the UPDATE_OBJECT splitting")
                    .WithButton("Enable split UPDATE_OBJECT", true)
                    .WithCancelButton(false)
                    .Build()))
                    return false;
                splitUpdate = true;
                await SplitPacketsIfNeededAsync();
                RaisePropertyChanged(nameof(SplitUpdate));
            }

            return true;
        }

        private int? Min(int? a, int? b) => a.HasValue ? (b.HasValue ? Math.Min(a.Value, b.Value) : a) : b;
        private int? Max(int? a, int? b) => a.HasValue ? (b.HasValue ? Math.Max(a.Value, b.Value) : a) : b;

        private Task SplitPacketsIfNeededAsync()
        {
            if (!splitUpdate)
                return Task.CompletedTask;
            
            if (AllPacketsSplit != null) 
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                AllPacketsSplit = new();
                var splitter = new SplitUpdateProcessor(new GuidExtractorProcessor());
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

                var finalized = splitter.Finalize();
                if (finalized != null)
                    foreach (var split in finalized)
                        AllPacketsSplit.Add(packetViewModelCreator.Process(split)!);
            });
        }

        public async Task<string?> RunProcessorsThreaded(IList<ProcessorViewModel> processors, CancellationToken cancellationToken)
        {
            List<(ProcessorViewModel viewModel, IPacketTextDumper dumper)> dumpers = new();
            foreach (var processor in processors)
                dumpers.Add((processor, await processor.CreateProcessor()));

            if (dumpers.Any(d => d.dumper.RequiresSplitUpdateObject))
                if (!await EnsureSplitOrDismiss())
                    return null;

            return await Task.Run(() =>
            {
                StringBuilder output = new();
                var packetsCount = (long)FilteredPackets.Count * processors.Count;
                long i = 0;
                foreach (var pair in dumpers)
                {
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    if (pair.dumper is ITwoStepPacketBoolProcessor preprocessor)
                    {
                        packetsCount *= 2;
                        foreach (var packet in FilteredPackets)
                        {
                            i++;
                            preprocessor.PreProcess(packet.Packet);
                            if ((i % 100) == 0)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                    break;
                                Report(i * 1.0f / packetsCount);
                            }
                        }
                    }

                    if (pair.dumper is IUnfilteredPacketProcessor unfilteredPacketProcessor)
                    {
                        int j = 0;
                        var all = splitUpdate ? AllPacketsSplit! : AllPackets!;
                        for (var index = 0; index < FilteredPackets.Count; index++)
                        {
                            var packet = FilteredPackets[index];

                            while (j < all.Count && all[j].Id < packet.Id)
                            {
                                unfilteredPacketProcessor.ProcessUnfiltered(all[j++].Packet);
                            }
                                
                            i++;
                            j++;
                            pair.dumper.Process(packet.Packet);
                            if ((i % 100) == 0)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                    break;
                                Report(i * 1.0f / packetsCount);
                            }
                        }
                    }
                    else
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
                    }

                    Report(-1);
                    
                    var task = pair.dumper.Generate();
                    task.Wait(cancellationToken);
                    
                    if (processors.Count > 1)
                        output.AppendLine(" -- " + pair.viewModel.Name);
                    output.Append(task.Result);

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
                var packets = await sniffLoader.LoadSniff(solutionItem.File, solutionItem.CustomVersion, currentActionToken.Token, this);

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
        
        private async Task<IFilterData?> GetRelatedFilters(int start, CancellationToken token)
        {
            try
            {
                var all = splitUpdate ? AllPacketsSplit! : AllPackets!;
                return await Task.Run(() =>
                    relatedPacketsFinder.Find(filteredPackets, all, start, token), token);
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (RelatedPacketsFinder.NoRelatedActionsException)
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Related packets finder")
                    .SetMainInstruction("No action found")
                    .SetContent("Couldn't find any action recognized by the editor in the selected packed ;(")
                    .WithOkButton(true)
                    .Build());
            }

            return null;
        }
        
        private async Task FilterPackets(string filter)
        {
            FilteringInProgress = true;
            var tokenSource = new CancellationTokenSource();
            filteringToken = tokenSource;    
            AssertNoOnGoingTask();
            currentActionToken = tokenSource;

            try
            {
                var previouslySelected = selectedPacket;
                var result = await filteringService.Filter(splitUpdate && AllPacketsSplit != null ? AllPacketsSplit : AllPackets, DisableFilters ? "" : filter, DisableFilters ? null : FilterData, tokenSource.Token, this);
                if (result != null)
                {
                    FilteredPackets = result;
                    VisiblePackets = HidePlayerMove
                        ? new ObservableCollection<PacketViewModel>(result.Where(r => !IsPlayerMovePacket(r)))
                        : result;
                    SelectedPacket = previouslySelected;
                    if (VisiblePackets.Count > 0)
                    {
                        DateTime prevTime = VisiblePackets[0].Time;
                        foreach (var packet in VisiblePackets)
                        {
                            packet.Diff = (int)packet.Time.Subtract(prevTime).TotalMilliseconds;
                            prevTime = packet.Time;
                        }
                    }

                    if (ReasonPanelVisibility)
                    {
                        actionReactionProcessor = actionReactionProcessorCreator.Create();
                        foreach (var f in filteredPackets)
                            actionReactionProcessor.PreProcess(f.Packet);
                        foreach (var f in filteredPackets)
                            actionReactionProcessor.Process(f.Packet);
                    }
                    else
                        actionReactionProcessor = null;
                }
            }
            catch (Exception e)
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
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

        private bool IsPlayerMovePacket(PacketViewModel packetViewModel)
        {
            return packetViewModel.Opcode.StartsWith("CMSG_MOVE_") ||
                   packetViewModel.Opcode.StartsWith("SMSG_MOVE_");
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

        private ActionReactionProcessor? actionReactionProcessor;
        
        private ObservableCollection<PacketViewModel> filteredPackets;
        public ObservableCollection<PacketViewModel> FilteredPackets
        {
            get => filteredPackets;
            set => SetProperty(ref filteredPackets, value);
        }

        private ObservableCollection<PacketViewModel> visiblePackets;
        public ObservableCollection<PacketViewModel> VisiblePackets
        {
            get => visiblePackets;
            set => SetProperty(ref visiblePackets, value);
        }

        private PacketViewModel? selectedPacket;
        public PacketViewModel? SelectedPacket
        {
            get => selectedPacket;
            set => SetPropertyWithOldValue(ref selectedPacket, value);
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
        
        private bool hidePlayerMove;
        public bool HidePlayerMove
        {
            get => hidePlayerMove;
            set => SetProperty(ref hidePlayerMove, value);
        }
        
        private bool disableFilters;
        public bool DisableFilters
        {
            get => disableFilters;
            set => SetProperty(ref disableFilters, value);
        }
        
        private bool findPanelEnabled;
        public bool FindPanelEnabled
        {
            get => findPanelEnabled;
            set => SetProperty(ref findPanelEnabled, value);
        }
        
        private bool wrapLines;
        public bool WrapLines
        {
            get => wrapLines;
            set => SetProperty(ref wrapLines, value);
        }

        private bool reasonPanelVisibility;
        public bool ReasonPanelVisibility
        {
            get => reasonPanelVisibility;
            set => SetProperty(ref reasonPanelVisibility, value);
        }

        private bool showSplitUpdateTip;
        public bool ShowSplitUpdateTip
        {
            get => showSplitUpdateTip;
            set => SetProperty(ref showSplitUpdateTip, value);
        }
        #endregion

        private bool inApplyFilterCommand;
        private CancellationTokenSource? filteringToken;
        private CancellationTokenSource? currentActionToken;
        public AsyncAutoCommand RunProcessors { get; }
        public AsyncAutoCommand<ProcessorViewModel> QuickRunProcessor { get; }
        public ObservableCollection<ProcessorViewModel> Processors { get; } = new();
        private ObservableCollectionExtended<PacketViewModel> AllPackets { get; } = new ();
        private ObservableCollectionExtended<PacketViewModel>? AllPacketsSplit { get; set; }
        public ReadOnlyObservableCollection<string> MostRecentlySearched { get; }
        public ObservableCollection<ActionReasonPredictionViewModel> Predictions { get; } = new();
        public ObservableCollection<PossibleActionViewModel> PossibleActions { get; } = new();
        public ObservableCollection<DetectedActionViewModel> DetectedActions { get; } = new();
        public ObservableCollection<DetectedEventViewModel> DetectedEvents { get; } = new();
        
        public IFilterData FilterData { get; set; } = new FilterData();
        public ICommand ToggleFindCommand { get; }
        public ICommand CloseFindCommand { get; }
        public AsyncAutoCommand<string> FindPreviousCommand { get; }
        public AsyncAutoCommand<string> FindNextCommand { get; }
        public AsyncAutoCommand OpenFilterDialogCommand { get; }
        public DelegateCommand<int?> JumpToPacketCommand { get; }
        public AsyncAutoCommand<PacketViewModel?> ExcludeGuidCommand { get; }
        public AsyncAutoCommand<PacketViewModel?> IncludeGuidCommand { get; }
        public AsyncAutoCommand<PacketViewModel?> ExcludeOpcodeCommand { get; }
        public AsyncAutoCommand<PacketViewModel?> IncludeOpcodeCommand { get; }
        public AsyncAutoCommand<PacketViewModel?> ExcludeEntryCommand { get; }
        public AsyncAutoCommand<PacketViewModel?> IncludeEntryCommand { get; }
        public AsyncAutoCommand FindRelatedPacketsCommands { get; }
        public AsyncAutoCommand ExplainSelectedPacketCommand { get; }
        public AsyncAutoCommand GoToPacketCommand { get; }
        public ICommand OpenHelpCommand { get; }
        public IAsyncCommand SaveToFileCommand { get; }
        public AsyncCommand ApplyFilterCommand { get; }
        public INativeTextDocument SelectedPacketPreview { get; }
        public INativeTextDocument FilterText { get; }
        public string Title { get; }
        public ImageUri? Icon => new ImageUri("Icons/document_sniff.png");
        public DelegateCommand UndoCommand { get; }
        public DelegateCommand RedoCommand { get; }
        public ICommand Undo => UndoCommand;
        public ICommand Redo => RedoCommand;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save => AlwaysDisabledCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; }
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History { get; }
        public ISolutionItem SolutionItem { get; }
        public Task<string> GenerateQuery() => Task.FromResult("");

        public bool ShowExportToolbarButtons => false;
    }
}