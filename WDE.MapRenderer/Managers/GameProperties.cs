using WDE.Module.Attributes;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

[UniqueProvider]
public interface IGameProperties
{
    bool OverrideLighting { get; }
    bool DisableTimeFlow { get; }
    int TimeSpeedMultiplier { get; }
    bool ShowGrid { get; }
    Time CurrentTime { get; set; }
    float ViewDistanceModifier { get; }
    bool ShowAreaTriggers { get; }
    int TextureQuality { get; }
    float DynamicResolution { get; }
}

[AutoRegister]
[SingleInstance]
public class GameProperties : IGameProperties
{
    public bool OverrideLighting { get; set; }
    public bool DisableTimeFlow { get; set; }
    public int TimeSpeedMultiplier { get; set; }
    public bool ShowGrid { get; set; }
    public Time CurrentTime { get; set; }
    public float ViewDistanceModifier { get; set; }
    public int TextureQuality { get; set; }
    public bool ShowAreaTriggers { get; set; }
    public float DynamicResolution { get; set; } = 1;
}