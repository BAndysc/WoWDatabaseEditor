using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using EnvDTE;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.SourceCode.Openers;

[AutoRegister]
[SingleInstance]
internal class VisualStudioOpener : IFileOpener
{
    [DllImport("ole32.dll")]
    private static extern void CreateBindCtx(int reserved, out IBindCtx ppbc);

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

    public string Name => "Visual Studio";

    public int Order => 0;

    public bool TryOpen(FileInfo path, int lineNumber)
    {
        if (!OperatingSystem.IsWindows())
            return false;

        var instances = GetVisualStudioInstances().ToList();

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

    private static IEnumerable<DTE> GetVisualStudioInstances()
    {
        IRunningObjectTable rot;
        IEnumMoniker enumMoniker;
        int retVal = GetRunningObjectTable(0, out rot);

        if (retVal == 0)
        {
            rot.EnumRunning(out enumMoniker);

            IMoniker[] moniker = new IMoniker[1];
            while (enumMoniker.Next(1, moniker, IntPtr.Zero) == 0)
            {
                CreateBindCtx(0, out var bindCtx);
                moniker[0].GetDisplayName(bindCtx, null, out var displayName);
                bool isVisualStudio = displayName.StartsWith("!VisualStudio");
                if (isVisualStudio)
                {
                    rot.GetObject(moniker[0], out var obj);
                    if (obj is DTE dte)
                        yield return dte;
                }
            }
        }
    }
}