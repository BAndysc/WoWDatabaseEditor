using System;
using System.IO;
using System.Linq;
using EnvDTE;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.COM;

namespace WDE.SourceCodeIntegrationEditor.SourceCode.Openers;

[AutoRegister]
[SingleInstance]
internal class VisualStudioOpener : IFileOpener
{
    public string Name => "Visual Studio";

    public int Order => 0;

    public bool TryOpen(FileInfo path, int lineNumber)
    {
        if (!OperatingSystem.IsWindows())
            return false;

        var instances = VisualStudioHelper.GetVisualStudioInstances().ToList();

        bool TryOpenInInstance(DTE dte)
        {
            var fileWindow = dte.ItemOperations.OpenFile(path.FullName);
            if (fileWindow == null)
                return false;

            if (fileWindow.Selection is TextSelection textSelection)
                textSelection.GotoLine(lineNumber);

            dte.MainWindow.Activate();
            return true;
        }

        foreach (var dte in instances.Where(inst => inst.Solution.FindProjectItem(path.FullName) != null))
        {
            if (TryOpenInInstance(dte))
                return true;
        }

        // if no solution is open, try to open in any instance
        foreach (var dte in instances)
        {
            if (TryOpenInInstance(dte))
                return true;
        }

        return false;
    }

}