using System.IO;
using System.Windows.Input;
using WDE.Common;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Types;
using WDE.Common.Utils;

namespace WDE.SourceCodeIntegrationEditor.SourceCode;

internal class SourceCodeFindResult : IFindAnywhereResult
{
    public SourceCodeFindResult(AsyncAutoCommand<SourceCodeFindResult> openCommand, FileInfo path, int lineNumber,
        string line)
    {
        CustomCommand = openCommand;
        Path = path;
        Title = path.Name + ":" + lineNumber;
        Description = line;
        LineNumber = lineNumber;
    }

    public ImageUri Icon { get; } = new ("Icons/document_cpp.png");
    public long? Entry => null;
    public string Title { get; }
    public string Description { get; }
    public ISolutionItem? SolutionItem => null;
    public ICommand? CustomCommand { get; }
    public FileInfo Path { get; }
    public int LineNumber { get; }
}