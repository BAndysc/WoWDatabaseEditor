using System.Threading.Tasks;

namespace WoWDatabaseEditorCore.ViewModels
{
    public interface ICloseAwareViewModel
    {
        Task<bool> CanClose();
    }
}