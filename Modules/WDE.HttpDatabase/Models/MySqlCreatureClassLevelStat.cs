
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonCreatureClassLevelStat : ICreatureClassLevelStat
    {
        
        public byte Level { get; set; }
        
        
        public byte Class { get; set; }
        
        
        public ushort BaseHp0 { get; set; }
        
        
        public ushort BaseHp1 { get; set; }
        
        
        public ushort BaseHp2 { get; set; }
        
        
        public int BaseMana { get; set; }
        
        
        public int BaseArmor { get; set; }
        
        
        public int AttackPower { get; set; }
        
        
        public int RangedAttackPower { get; set; }
        
        
        public float DamageBase { get; set; }
        
        
        public float DamageExp1 { get; set; }
        
        
        public float DamageExp2 { get; set; }

        public uint BaseHp(byte expansion)
        {
            if (expansion == 0)
                return BaseHp0;
            if (expansion == 1)
                return BaseHp1;
            if (expansion == 2)
                return BaseHp2;
            return 1;
        }

        public float Damage(byte expansion)
        {
            if (expansion == 0)
                return DamageBase;
            if (expansion == 1)
                return DamageExp1;
            if (expansion == 2)
                return DamageExp2;
            return 1;
        }
    }
}