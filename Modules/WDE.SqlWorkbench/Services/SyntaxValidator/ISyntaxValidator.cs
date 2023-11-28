using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaEdit.Document;

namespace WDE.SqlWorkbench.Services.SyntaxValidator;

internal interface ISyntaxValidator
{
    Task ValidateAsync(ITextSource source, int start, int length, List<SyntaxError> output, CancellationToken cancellationToken);
}