using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Services;

[UniqueProvider]
public interface IFavouriteSmartsService
{
    bool IsFavourite(string name);
    void SetFavourite(string name, bool @is);
}