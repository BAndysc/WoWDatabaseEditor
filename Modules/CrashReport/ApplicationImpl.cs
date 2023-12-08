using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using WDE.Common.Managers;

namespace CrashReport;

public class ApplicationImpl : IApplication
{
    public async Task<bool> CanClose()
    {
        return true;
    }

    public async Task<bool> TryClose()
    {
        ForceClose();
        return true;
    }

    public void ForceClose()
    {
        ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)?.Shutdown();
    }
}