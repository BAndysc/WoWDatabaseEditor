namespace WDE.Common.Database
{
    public interface IDatabaseSpellDbc
    {
        uint Id { get; }
        string? Name { get; }
    }
    
    public interface IDatabaseSpellEffectDbc
    {
        uint Id { get; }
        uint SpellId { get; }
        uint Effect { get; }
        int BasePoints { get; }
        uint EffectIndex { get; }
        uint Aura { get; }
        int EffectMiscValue { get; }
        int EffectMiscValueB { get; }
        uint ImplicitTarget1 { get; }
        uint ImplicitTarget2 { get; }
    }
}