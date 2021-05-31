using System.Collections.Generic;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.Services
{
    public class CreatureStatCalculatorService : ICreatureStatCalculatorService
    {
        private readonly IDatabaseProvider databaseProvider;
        private Dictionary<int, ICreatureClassLevelStat> stats = new();
        private bool built = false;

        public CreatureStatCalculatorService(IDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
        }
        
        public uint GetHealthFor(byte level, byte unitClass, byte expansion)
        {
            EnsureBuildStats();

            if (!stats.TryGetValue(MakeKey(level, unitClass), out var stat))
                return 1;
            
            return stat.BaseHp(expansion);
        }

        public int GetAttackPowerBonusFor(byte level, byte unitClass)
        {
            EnsureBuildStats();

            if (!stats.TryGetValue(MakeKey(level, unitClass), out var stat))
                return 0;

            return stat.AttackPower;
        }

        public int GetRangedAttackPowerBonusFor(byte level, byte unitClass)
        {
            EnsureBuildStats();

            if (!stats.TryGetValue(MakeKey(level, unitClass), out var stat))
                return 0;

            return stat.RangedAttackPower;
        }
        
        public int GetManaFor(byte level, byte unitClass)
        {
            EnsureBuildStats();

            if (!stats.TryGetValue(MakeKey(level, unitClass), out var stat))
                return 0;

            return stat.BaseMana;
        }

        public int GetArmorFor(byte level, byte unitClass)
        {
            EnsureBuildStats();

            if (!stats.TryGetValue(MakeKey(level, unitClass), out var stat))
                return 0;

            return stat.BaseArmor;
        }

        public float GetDamageFor(byte level, byte unitClass, byte expansion)
        {
            EnsureBuildStats();

            if (!stats.TryGetValue(MakeKey(level, unitClass), out var stat))
                return 1;

            return stat.Damage(expansion);
        }
        
        private void EnsureBuildStats()
        {
            if (built) 
                return;
            
            built = true;
            CacheStats();
        }

        private void CacheStats()
        {
            foreach (var stat in databaseProvider.GetCreatureClassLevelStats())
            {
                stats[MakeKey(stat)] = stat;
            }
        }

        private int MakeKey(ICreatureClassLevelStat stat)
        {
            return MakeKey(stat.Level, stat.Class);
        }
        
        private int MakeKey(byte level, byte @class)
        {
            return ((int)level) << 8 | @class;
        }
    }
}