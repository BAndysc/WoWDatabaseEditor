using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.SourceCode.Openers;

namespace WDE.SourceCodeIntegrationEditor.SourceCode;

[AutoRegister]
[SingleInstance]
internal class SourceCodeFindAnywhereSource : IFindAnywhereSource
{
    private readonly ISourceCodePathService sourceCodePathService;
    private readonly IMainThread mainThread;
    private readonly IFileOpenerService fileOpenerService;
    private readonly IMessageBoxService messageBoxService;
    public FindAnywhereSourceType SourceType => FindAnywhereSourceType.SourceCode;
    public int Order => 50;

    private AsyncAutoCommand<SourceCodeFindResult> OpenCommand { get; }

    public SourceCodeFindAnywhereSource(ISourceCodePathService sourceCodePathService,
        IMainThread mainThread,
        IFileOpenerService fileOpenerService,
        IMessageBoxService messageBoxService)
    {
        this.sourceCodePathService = sourceCodePathService;
        this.mainThread = mainThread;
        this.fileOpenerService = fileOpenerService;
        this.messageBoxService = messageBoxService;

        OpenCommand = new AsyncAutoCommand<SourceCodeFindResult>(async r =>
        {
            if (!fileOpenerService.TryOpen(r.Path, r.LineNumber))
            {
                var methods = string.Join(", ", fileOpenerService.OpeningMethodNames);
                await this.messageBoxService.SimpleDialog("Error", "Cannot open file", $"None of the supported file open methods succeeded (tried: {methods}) :(");
            }
        });
    }

    class TemporaryData
    {
        public readonly byte[] Buffer = new byte[1024 * 10];
        public readonly List<(long, long, string)> Results = new();
    }

    public async Task Find(IFindAnywhereResultContext resultContext, FindAnywhereSourceType searchType, IReadOnlyList<string> parameterNames,
        long parameterValue, CancellationToken cancellationToken)
    {
        if (parameterValue < 10)
            return; // this would yield too many false positives

        var valueString = parameterValue.ToString();
        var valueBytes = Encoding.UTF8.GetBytes(valueString);
        var valueLength = valueBytes.Length;
        foreach (var path in sourceCodePathService.SourceCodePaths)
        {
            var files = Enumerable.Concat(Directory.GetFiles(path, "*.h", SearchOption.AllDirectories),
                    Directory.GetFiles(path, "*.cpp", SearchOption.AllDirectories))
                .ToArray();

            ParallelOptions options = new()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            await Task.Run(() =>
            {
                Parallel.ForEach(files, options, () => new TemporaryData(), (f, loop, data) =>
                {
                    data.Results.Clear();
                    FastByteSearcher.IndexOfWholeWorldForAll(f, valueBytes, data.Buffer, data.Results);

                    foreach (var (lineNumber, offset, line) in data.Results)
                    {
                        mainThread.Dispatch(() =>
                        {
                            resultContext.AddResult(new SourceCodeFindResult(OpenCommand, new FileInfo(f), (int)lineNumber + 1, line));
                        });
                    }

                    return data;
                }, (data) => { data.Results.Clear(); });
            }, cancellationToken);
        }
    }
}