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

        ushort BaseHp(byte expansion);
        float Damage(byte expansion);
    }
}