using WDE.Module.Attributes;

namespace WDE.Common.Managers
{
    [UniqueProvider]
    public interface IWindowManager
    {
        bool ShowDialog(IDialog viewModel);
    }
}