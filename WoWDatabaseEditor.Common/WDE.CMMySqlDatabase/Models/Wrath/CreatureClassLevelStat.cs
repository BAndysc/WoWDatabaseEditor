using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models.Wrath
{
    [Table(Name = "creature_template_classlevelstats")]
    public class CreatureClassLevelStatWoTLK : ICreatureClassLevelStat
    {
        [Column("Level", IsPrimaryKey = true, PrimaryKeyOrder = 0)] public byte   Level                 { get; set; } // tinyint(4)
        [Column("Class", IsPrimaryKey = true, PrimaryKeyOrder = 1)] public byte   Class                 { get; set; } // tinyint(4)
        [Column("BaseHealthExp0")                                 ] public ushort BaseHp0               { get; set; } // mediumint(8) unsigned
        [Column("BaseHealthExp1")                                 ] public ushort BaseHp1               { get; set; } // mediumint(8) unsigned
        [Column("BaseHealthExp2")                                 ] public ushort BaseHp2               { get; set; } // mediumint(8) unsigned
        [Column("BaseMana")                                       ] public int BaseMana              { get; set; } // mediumint(8) unsigned
        [Column("BaseDamageExp0")                                 ] public float  DamageBase            { get; set; } // float
        [Column("BaseDamageExp1")                                 ] public float  DamageExp1            { get; set; } // float
        [Column("BaseDamageExp2")                                 ] public float  DamageExp2            { get; set; } // float
        [Column("BaseMeleeAttackPower")                           ] public float  fAttackPower          { get; set; } // float
        [Column("BaseRangedAttackPower")                          ] public float  fRangedAttackPower    { get; set; } // float
        [Column("BaseArmor")                                      ] public int BaseArmor             { get; set; } // mediumint(8) unsigned

        public int AttackPower => (ushort)(fAttackPower > 0.0 ? System.Convert.ToUInt16(fAttackPower) : 0);
        public int RangedAttackPower => (ushort)(fRangedAttackPower > 0.0 ? System.Convert.ToUInt16(fRangedAttackPower) : 0);

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