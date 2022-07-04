namespace WDE.Common.Database
{
    public interface ICreatureClassLevelStat
    {
        byte Level { get; }
        byte Class { get; }
        int BaseMana { get; }
        int BaseArmor { get; }
        int AttackPower { get; }
        int RangedAttackPower { get; }

        uint BaseHp(byte expansion);
        float Damage(byte expansion);
    }
}