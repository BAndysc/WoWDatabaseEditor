using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WDE.Common;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.SourceCode.Openers;

[AutoRegister]
[SingleInstance]
internal class FileOpenerService : IFileOpenerService
{
    private readonly List<IFileOpener> openers;

    public FileOpenerService(IEnumerable<IFileOpener> openers)
    {
        this.openers = openers.OrderBy(x => x.Order).ToList();
        OpeningMethodNames = this.openers.Select(x => x.Name).ToList();
    }

    public bool TryOpen(FileInfo path, int lineNumber)
    {
        foreach (var opener in openers)
        {
            try
            {
                if (opener.TryOpen(path, lineNumber))
                    return true;
            }
            catch (Exception e)
            {
                LOG.LogWarning(e);
            }
        }

        return false;
    }

    public IEnumerable<string> OpeningMethodNames { get; }
}