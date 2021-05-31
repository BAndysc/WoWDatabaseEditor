using WDE.Module.Attributes;

namespace WDE.Common.Database
{
    [UniqueProvider]
    public interface ICreatureStatCalculatorService
    {
        uint GetHealthFor(byte level, byte unitClass, byte expansion);
        int GetManaFor(byte level, byte unitClass);
        int GetArmorFor(byte level, byte unitClass);
        int GetAttackPowerBonusFor(byte level, byte unitClass);
        int GetRangedAttackPowerBonusFor(byte level, byte unitClass);
        float GetDamageFor(byte level, byte unitClass, byte expansion);
    }
}