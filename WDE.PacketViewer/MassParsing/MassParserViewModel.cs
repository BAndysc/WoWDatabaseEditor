using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using DynamicData;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.PacketViewer.PacketParserIntegration;
using WDE.PacketViewer.Processing;
using WDE.PacketViewer.Processing.ProcessorProviders;
using WDE.PacketViewer.Utils;
using WDE.PacketViewer.ViewModels;

namespace WDE.PacketViewer.MassParsing;

public partial class MassParserViewModel : ObservableBase, IDocument
{
    private readonly IMessageBoxService messageBoxService;
    private readonly ISniffLoader sniffLoader;
    private readonly IDocumentManager documentManager;
    private readonly IMainThread mainThread;
    private readonly ITextDocumentService textDocumentService;
    private readonly IParsingSettings parsingSettings;
    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public IHistoryManager? History { get; set; }
    public bool IsModified { get; set; }
    public string Title => "Mass sniff parsing";
    public ICommand Copy => AlwaysDisabledCommand.Command;
    public ICommand Cut => AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save=> AlwaysDisabledAsyncCommand.Command;
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
    public ImageUri? Icon => new ImageUri("Icons/document_mass_sniff.png");

    public ObservableCollection<string> FileItems { get; } = new();
    public List<ProcessorViewModel> Processors { get; } = new();
    public ICommand AddFileCommand { get; }
    public ICommand RemoveFileCommand { get; }
    public ICommand AddFileRecursiveCommand { get; }
    public ICommand RunProcessor { get; }
    public DelegateCommand InterruptCurrentTaskCommand { get; }

    public List<string> SelectedFiles { get; set; } = new();

    [Notify] private string taskName = "";
    [Notify] private string currentSubTask = "";
    [Notify] private bool longTaskInProgress;
    [Notify] private CancellationTokenSource? longTaskToken;
    
    public MassParserViewModel(IEnumerable<IPacketDumperProvider> dumperProviders,
        IMessageBoxService messageBoxService,
        ISniffLoader sniffLoader,
        IDocumentManager documentManager,
        IMainThread mainThread,
        ITextDocumentService textDocumentService,
        IParsingSettings parsingSettings,
        IWindowManager windowManager)
    {
        this.messageBoxService = messageBoxService;
        this.sniffLoader = sniffLoader;
        this.documentManager = documentManager;
        this.mainThread = mainThread;
        this.textDocumentService = textDocumentService;
        this.parsingSettings = parsingSettings;
        foreach (var dumper in dumperProviders)
            if (!dumper.RequiresSplitUpdateObject && dumper.CanProcessMultipleFiles)
                Processors.Add(new (dumper));

        InterruptCurrentTaskCommand = new DelegateCommand(() =>
        {
            longTaskToken?.Cancel();
            longTaskToken = null;
        }, () => longTaskToken != null);
        On(() => LongTaskToken, _ => InterruptCurrentTaskCommand.RaiseCanExecuteChanged());
        AddFileCommand = new AsyncAutoCommand(async () =>
        {
            var file = await windowManager.ShowOpenFileDialog("Sniff file|pkt,bin");
            if (file == null)
                return;
            if (!File.Exists(file))
                return;
            if (FileItems.Contains(file))
                return;
            FileItems.Add(file);
        });
        RemoveFileCommand = new DelegateCommand(() =>
        {
            foreach (var selected in SelectedFiles.ToList())
                FileItems.Remove(selected);
        });
        AddFileRecursiveCommand = new AsyncAutoCommand( () =>
        {
            return WrapLongTask("Enumerating sniffs...", async (token, subTaskSetter) =>
            {
                var path = await windowManager.ShowFolderPickerDialog("");
                if (path == null)
                    return;
                List<string>? files = await Task.Run(() =>
                {
                    List<string> files = new();
                    int i = 0;
                    foreach (string file in Directory.EnumerateFiles(path, "*.pkt", SearchOption.AllDirectories))
                    {
                        i++;
                        if (token.IsCancellationRequested)
                            return null;

                        files.Add(file);
                        if (i % 50 == 0)
                            subTaskSetter(file);
                    }

                    return files;
                }, token);

                if (token.IsCancellationRequested)
                    return;

                if (files != null)
                {
                    for (var index = files.Count - 1; index >= 0; --index)
                    {
                        var f = files[index];
                        if (FileItems.Contains(f))
                            files.RemoveAt(index);
                    }

                    FileItems.AddRange(files);
                }
            });
        });

        RunProcessor = new AsyncAutoCommand<ProcessorViewModel>( dumper =>
        {
            return WrapLongTask("Running processors", async (token, subTaskSetter) =>
            {
                if (FileItems.Count == 0)
                    return;
                
                IPacketTextDumper? textProcessor = null;
                IPacketDocumentDumper? documentDumper = null;
                ITwoStepPacketBoolProcessor? twoStep = null;
                IPerFileStateProcessor? perFileStateProcessor = null;

                if (dumper.IsTextDumper)
                    textProcessor = await dumper.CreateTextProcessor(this.parsingSettings);
                else if (dumper.IsDocumentDumper)
                    documentDumper = await dumper.CreateDocumentProcessor(this.parsingSettings);

                if (textProcessor is ITwoStepPacketBoolProcessor twoStep_)
                    twoStep = twoStep_;
                else if (documentDumper is ITwoStepPacketBoolProcessor twoStep_2)
                    twoStep = twoStep_2;
                
                if (textProcessor is IPerFileStateProcessor perFileState_)
                    perFileStateProcessor = perFileState_;
                else if (documentDumper is IPerFileStateProcessor perFileState_2)
                    perFileStateProcessor = perFileState_2;

                var total = FileItems.Count;
                var i = 0;
                foreach (var file in FileItems.ToList())
                {
                    using var allocator = new RefCountedArena();
                    i++;
                    subTaskSetter(file + " (" + i + "/" + total + ")");
                    var packets = await sniffLoader.LoadSniff(allocator, file, null, token, false, new Progress<float>());
                    
                    if (perFileStateProcessor != null)
                        perFileStateProcessor.ClearAllState();
                    
                    textProcessor?.Initialize(packets.GameVersion);
                    documentDumper?.Initialize(packets.GameVersion);

                    if (twoStep != null)
                    {
                        void SynchronousPreprocess()
                        {
                            foreach (ref var packet in packets.Packets_.AsSpan())
                            {
                                twoStep.PreProcess(ref packet);
                            }
                        }
                        SynchronousPreprocess();

                        await twoStep.PostProcessFirstStep();
                    }


                    void SynchronousProcess()
                    {
                        foreach (ref var packet in packets.Packets_.AsSpan())
                        {
                            try
                            {
                                textProcessor?.Process(in packet);
                                documentDumper?.Process(in packet);
                            }
                            catch (Exception e)
                            {
                                LOG.LogError(e, "Error while processing packet");
                            }
                        }
                    }
                    SynchronousProcess();
                }

                if (textProcessor != null)
                {
                    await textProcessor.Process();
                    var textOutput = await textProcessor.Generate();
                    var name = dumper.Name;
                    var extension = dumper.Extension;
                    documentManager.OpenDocument(textDocumentService.CreateDocument( $"{name} ({Title})", textOutput, extension!, true));
                }
                else if (documentDumper != null)
                {
                    await documentDumper.Process();
                    var doc = await documentDumper.Generate(null);
                    documentManager.OpenDocument(doc);
                }
            });
        });
        CloseCommand = new AsyncAutoCommand(() =>
        {
            longTaskToken?.Cancel();
            longTaskToken = null;
            return Task.CompletedTask;
        });
    }

    private async Task WrapLongTask(string name, Func<CancellationToken, Action<string>, Task> t)
    {
        if (LongTaskInProgress)
            throw new Exception("Some other task is running");
        CurrentSubTask = "";
        TaskName = name;
        LongTaskInProgress = true;
        LongTaskToken = new CancellationTokenSource();
        try
        {
            await messageBoxService.WrapError(token => t(token, state =>
            {
                mainThread.Dispatch(() => CurrentSubTask = state);
            }))(LongTaskToken.Token);
        }
        catch (Exception e)
        {
            LOG.LogError(e, "Error while running long task");
            throw;
        }
        finally
        {
            LongTaskInProgress = false;
            LongTaskToken = null;
        }
    }
}
