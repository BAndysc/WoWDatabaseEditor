using System.Threading.Tasks;
using Avalonia.Input.Platform;

namespace WDE.Common.Avalonia.Utils
{
    public interface ICustomCopyPaste
    {
        Task DoPaste();
        void DoCopy();
    }
}