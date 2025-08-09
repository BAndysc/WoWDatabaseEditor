using System;
using System.Threading.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders;

[AutoRegister, SingleInstance]
public class SpawnDumperProvider : ITextPacketDumperProvider
{
    private readonly Func<SpawnDumper> factory;
    public string Name => "Spawns";

    public string Description => "Dump spawned creatures and game objects into SQL format.";

    public string Extension => "sql";

    public ImageUri? Image { get; } = new ImageUri("Icons/document_creature_summon_groups.png");

    public SpawnDumperProvider(Func<SpawnDumper> factory)
    {
        this.factory = factory;
    }

    public async Task<IPacketTextDumper> CreateDumper(IParsingSettings settings)
    {
        return await Task.FromResult<IPacketTextDumper>(factory());
    }
}