using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Providers;

[UniqueProvider]
public interface IRaceProviderService
{
    Task<CharacterRaces?> PickRaces(CharacterRaces currentRaces);
}