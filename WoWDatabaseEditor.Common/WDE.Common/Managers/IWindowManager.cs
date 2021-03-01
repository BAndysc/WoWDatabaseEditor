using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Managers
{
    [UniqueProvider]
    public interface IWindowManager
    {
        Task<bool> ShowDialog(IDialog viewModel);

        Task<string> ShowFolderPickerDialog(string defaultDirectory);
    }
}