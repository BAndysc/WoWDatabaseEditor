using WDE.Common.DBC.Structs;

namespace WDE.Common.DBC;

public interface IFactionTemplateStore
{
    FactionTemplate? GetFactionTemplate(uint templateId);
    Faction? GetFaction(ushort factionId);
}

public static class FactionTemplateExtensions
{
    public static Faction? GetFactionByTemplate(this IFactionTemplateStore store, uint templateId)
    {
        var template = store.GetFactionTemplate(templateId);
        if (!template.HasValue)
            return null;

        var faction = store.GetFaction(template.Value.Faction);
        return faction;
    }
}