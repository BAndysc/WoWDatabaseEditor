using WDE.Module.Attributes;

namespace WDE.EventAiEditor.Services;

[UniqueProvider]
public interface IFavouriteEventAiService
{
    bool IsFavourite(string name);
    void SetFavourite(string name, bool @is);
}