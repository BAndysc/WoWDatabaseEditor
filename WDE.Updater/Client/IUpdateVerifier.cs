using System.IO;
using System.Threading.Tasks;

namespace WDE.Updater.Client
{
    public interface IUpdateVerifier
    {
        Task<bool> IsUpdateValid(FileInfo file, string hash);
    }
}