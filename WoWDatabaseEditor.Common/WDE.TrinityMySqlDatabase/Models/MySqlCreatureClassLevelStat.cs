using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "creature_classlevelstats")]
    public class MySqlCreatureClassLevelStat : ICreatureClassLevelStat
    {
        [PrimaryKey]
        [Column(Name = "level")]
        public byte Level { get; set; }
        
        [PrimaryKey]
        [Column(Name = "class")]
        public byte Class { get; set; }
        
        [Column(Name = "basehp0")]
        public ushort BaseHp0 { get; set; }
        
        [Column(Name = "basehp1")]
        public ushort BaseHp1 { get; set; }
        
        [Column(Name = "basehp2")]
        public ushort BaseHp2 { get; set; }
        
        [Column(Name = "basemana")]
        public int BaseMana { get; set; }
        
        [Column(Name = "basearmor")]
        public int BaseArmor { get; set; }
        
        [Column(Name = "attackpower")]
        public int AttackPower { get; set; }
        
        [Column(Name = "rangedattackpower")]
        public int RangedAttackPower { get; set; }
        
        [Column(Name = "damage_base")]
        public float DamageBase { get; set; }
        
        [Column(Name = "damage_exp1")]
        public float DamageExp1 { get; set; }
        
        [Column(Name = "damage_exp2")]
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