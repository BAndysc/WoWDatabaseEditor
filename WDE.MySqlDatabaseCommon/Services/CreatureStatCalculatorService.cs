using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Utils;

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
            // @todo make it async
            EnsureBuildStats().AsTask().ListenErrors();

            if (!stats.TryGetValue(MakeKey(level, unitClass), out var stat))
                return 1;
            
            return stat.BaseHp(expansion);
        }

        public int GetAttackPowerBonusFor(byte level, byte unitClass)
        {
            // @todo make it async
            EnsureBuildStats().AsTask().ListenErrors();

            if (!stats.TryGetValue(MakeKey(level, unitClass), out var stat))
                return 0;

            return stat.AttackPower;
        }

        public int GetRangedAttackPowerBonusFor(byte level, byte unitClass)
        {
            // @todo make it async
            EnsureBuildStats().AsTask().ListenErrors();

            if (!stats.TryGetValue(MakeKey(level, unitClass), out var stat))
                return 0;

            return stat.RangedAttackPower;
        }
        
        public int GetManaFor(byte level, byte unitClass)
        {
            // @todo make it async
            EnsureBuildStats().AsTask().ListenErrors();

            if (!stats.TryGetValue(MakeKey(level, unitClass), out var stat))
                return 0;

            return stat.BaseMana;
        }

        public int GetArmorFor(byte level, byte unitClass)
        {
            // @todo make it async
            EnsureBuildStats().AsTask().ListenErrors();

            if (!stats.TryGetValue(MakeKey(level, unitClass), out var stat))
                return 0;

            return stat.BaseArmor;
        }

        public float GetDamageFor(byte level, byte unitClass, byte expansion)
        {
            // @todo make it async
            EnsureBuildStats().AsTask().ListenErrors();

            if (!stats.TryGetValue(MakeKey(level, unitClass), out var stat))
                return 1;

            return stat.Damage(expansion);
        }
        
        private async ValueTask EnsureBuildStats()
        {
            if (built) 
                return;
            
            built = true;
            await CacheStats();
        }

        private async Task CacheStats()
        {
            foreach (var stat in await databaseProvider.GetCreatureClassLevelStatsAsync())
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