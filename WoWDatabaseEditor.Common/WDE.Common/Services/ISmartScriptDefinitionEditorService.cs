using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface ISmartScriptDefinitionEditorService
{
    bool IsSupported { get; }
    void EditEvent(int id);
    void EditAction(int id);
    void EditTarget(int id);
    void Open();
}