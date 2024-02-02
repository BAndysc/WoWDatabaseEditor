using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

public interface IBaseSpellDebugger
{
    bool Enabled { get; }
    Task ToggleSpellDebugging(uint spellId);
    bool IsDebuggingSpell(uint spellId);
}

[FallbackAutoRegister]
public class BaseSpellDebugger : IBaseSpellDebugger
{
    public bool Enabled => false;
    public Task ToggleSpellDebugging(uint spellId) => Task.CompletedTask;
    public bool IsDebuggingSpell(uint spellId) => false;
}