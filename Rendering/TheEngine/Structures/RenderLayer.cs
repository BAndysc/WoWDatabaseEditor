namespace TheEngine.Structures;

public readonly record struct RenderLayer
{
    private readonly byte layer;
    private readonly int version;

    public RenderLayer(byte layer, int version)
    {
        this.layer = layer;
        this.version = version;
    }

    public byte Layer => layer;
    public int Version => version;

    public const int MAX_LAYERS = 0b111111;

    public static readonly RenderLayer Default = default;
    public bool IsDefault => layer == 0;
}