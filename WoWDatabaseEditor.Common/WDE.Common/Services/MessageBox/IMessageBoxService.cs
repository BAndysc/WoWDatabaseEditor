using System.Threading.Tasks;

namespace WDE.Common.Services.MessageBox
{
    public interface IMessageBoxService
    {
        Task<T> ShowDialog<T>(IMessageBox<T> messageBox);
    }
}