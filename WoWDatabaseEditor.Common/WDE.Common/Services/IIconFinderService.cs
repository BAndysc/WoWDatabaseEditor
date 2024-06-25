using System.IO;
using System.Threading.Tasks;
using WDE.Common.Types;

namespace WDE.Common.Services;

public interface IIconFinderService
{
    Task<ImageUri?> PickIconAsync();
    bool Enabled { get; }
}