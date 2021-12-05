using WDE.Module.Attributes;
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
    public bool ShowAreaTriggers { get; set; }
}