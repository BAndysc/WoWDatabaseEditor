using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.SkyFireMySqlDatabase.Models
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
        
        [Column(Name = "OldContentBaseHp")]
        public ushort BaseHp0 { get; set; }
        
        [Column(Name = "OldContentBaseHp")]
        public ushort BaseHp1 { get; set; }
        
        [Column(Name = "OldContentBaseHp")]
        public ushort BaseHp2 { get; set; }
        
        [Column(Name = "basemana")]
        public ushort BaseMana { get; set; }
        
        [Column(Name = "basearmor")]
        public ushort BaseArmor { get; set; }

        public ushort AttackPower => 0;

        public ushort RangedAttackPower => 0;

        public float DamageBase => 0;

        public float DamageExp1 => 0.0f;

        public float DamageExp2 => 0.0f;

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