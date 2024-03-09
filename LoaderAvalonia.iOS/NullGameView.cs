using WDE.Common.Services;
using WDE.Module.Attributes;

namespace LoaderAvalonia.iOS;

[AutoRegister]
[SingleInstance]
public class NullGameView : IGameViewService
{
    public bool IsSupported => false;

    public void Open()
    {

    }
}