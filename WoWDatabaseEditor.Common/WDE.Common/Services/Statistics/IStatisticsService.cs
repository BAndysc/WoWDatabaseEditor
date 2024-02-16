using WDE.Module.Attributes;

namespace WDE.Common.Services.Statistics;

[UniqueProvider]
public interface IStatisticsService
{
    public ulong RunCounter { get; }
}
