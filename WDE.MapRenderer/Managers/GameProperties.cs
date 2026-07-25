using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

[UniqueProvider]
public interface IGameProperties
{
    bool OverrideLighting { get; set; }
    bool DisableTimeFlow { get; set;}
    int TimeSpeedMultiplier { get; set; }
    bool ShowGrid { get; set; }
    Time CurrentTime { get; set; }
    float ViewDistanceModifier { get; set;}
    bool ShowAreaTriggers { get; set; }
    bool ShowStatusIcons { get; set; }
    /// <summary>Bitmask of hidden <see cref="StatusIconsManager.StatusIcon"/> bits. Stored as the
    /// HIDDEN set (not the shown one) so icon kinds added later default to visible.</summary>
    uint StatusIconsHiddenMask { get; set; }
    int TextureQuality { get; set; }
    /// <summary>Persisted vsync wish. Only effective when the engine's present target supports
    /// control (the native panel / standalone window); the composition panel is always vsynced.</summary>
    bool VSync { get; set; }
    float DynamicResolution { get; set; }
    bool RenderGui { get; set; }
    bool LoadWorld { get; }
}

[AutoRegister]
[SingleInstance]
public class GameProperties : IGameProperties
{
    private readonly GameViewSettings settings;
    private readonly IMainThread mainThread;

    private bool overrideLighting;
    private bool disableTimeFlow;
    private int timeSpeedMultiplier;
    private bool showGrid;
    private Time currentTime;
    private float viewDistanceModifier;
    private int textureQuality;
    private bool showAreaTriggers;
    private bool showStatusIcons;
    private uint statusIconsHiddenMask;
    private bool vSync;

    public GameProperties(GameViewSettings settings, IMainThread mainThread)
    {
        this.settings = settings;
        this.mainThread = mainThread;
        overrideLighting = settings.OverrideLighting;
        disableTimeFlow = settings.DisableTimeFlow;
        timeSpeedMultiplier = settings.TimeSpeedMultiplier;
        showGrid = settings.ShowGrid;
        currentTime = Time.FromMinutes(settings.CurrentTime);
        viewDistanceModifier = settings.ViewDistanceModifier;
        textureQuality = settings.TextureQuality;
        showAreaTriggers = settings.ShowAreaTriggers;
        showStatusIcons = settings.ShowStatusIcons;
        statusIconsHiddenMask = settings.StatusIconsHiddenMask;
        vSync = settings.VSync;
    }

    // setters run on the game thread (the in-view ImGui toolbar); the settings file write must
    // happen on the main thread (IUserSettings is not thread safe)
    private void Persist(Action save) => mainThread.Dispatch(save);

    public bool OverrideLighting
    {
        get => overrideLighting;
        set
        {
            overrideLighting = value;
            Persist(() => settings.OverrideLighting = value);
        }
    }

    public bool DisableTimeFlow
    {
        get => disableTimeFlow;
        set
        {
            disableTimeFlow = value;
            Persist(() => settings.DisableTimeFlow = value);
        }
    }

    public int TimeSpeedMultiplier
    {
        get => timeSpeedMultiplier;
        set
        {
            timeSpeedMultiplier = value;
            Persist(() => settings.TimeSpeedMultiplier = value);
        }
    }

    public bool ShowGrid
    {
        get => showGrid;
        set
        {
            showGrid = value;
            Persist(() => settings.ShowGrid = value);
        }
    }

    public Time CurrentTime
    {
        get => currentTime;
        set
        {
            currentTime = value;
            var minutes = value.TotalMinutes;
            Persist(() => settings.CurrentTime = minutes);
        }
    }

    public float ViewDistanceModifier
    {
        get => viewDistanceModifier;
        set
        {
            viewDistanceModifier = value;
            Persist(() => settings.ViewDistanceModifier = value);
        }
    }

    public int TextureQuality
    {
        get => textureQuality;
        set
        {
            textureQuality = value;
            Persist(() => settings.TextureQuality = value);
        }
    }

    public bool ShowAreaTriggers
    {
        get => showAreaTriggers;
        set
        {
            showAreaTriggers = value;
            Persist(() => settings.ShowAreaTriggers = value);
        }
    }

    public bool ShowStatusIcons
    {
        get => showStatusIcons;
        set
        {
            showStatusIcons = value;
            Persist(() => settings.ShowStatusIcons = value);
        }
    }

    public uint StatusIconsHiddenMask
    {
        get => statusIconsHiddenMask;
        set
        {
            statusIconsHiddenMask = value;
            Persist(() => settings.StatusIconsHiddenMask = value);
        }
    }

    public bool VSync
    {
        get => vSync;
        set
        {
            vSync = value;
            Persist(() => settings.VSync = value);
        }
    }

    public float DynamicResolution { get; set; } = 1;
    public bool RenderGui { get; set; } = true;
    public bool LoadWorld => true;
}
