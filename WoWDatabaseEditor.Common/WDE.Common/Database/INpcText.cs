using WDE.Common.Annotations;

namespace WDE.Common.Database
{
    public interface INpcText
    {
        uint Id { get; }
        [CanBeNull] string Text0_0 { get; }
        [CanBeNull] string Text0_1 { get; }
    }
}