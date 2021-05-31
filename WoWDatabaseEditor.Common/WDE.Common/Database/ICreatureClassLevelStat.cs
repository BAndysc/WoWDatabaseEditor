namespace WDE.Common.Database
{
    public interface ICreatureClassLevelStat
    {
        byte Level { get; }
        byte Class { get; }
        ushort BaseMana { get; }
        ushort BaseArmor { get; }
        ushort AttackPower { get; }
        ushort RangedAttackPower { get; }

        uint BaseHp(byte expansion);
        float Damage(byte expansion);
    }
}