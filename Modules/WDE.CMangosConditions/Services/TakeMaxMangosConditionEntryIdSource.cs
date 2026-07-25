using System;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services.IdGenerator;
using WDE.Module.Attributes;

namespace WDE.CMangosConditions.Services
{
    [AutoRegister]
    [SingleInstance]
    [RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
    internal class TakeMaxMangosConditionEntryIdSource : IdSource<MangosConditionEntryIdType>
    {
        private readonly IMangosDatabaseProvider databaseProvider;

        public TakeMaxMangosConditionEntryIdSource(IMangosDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
        }

        public override string Name => "Max condition_entry in the database + 1";

        protected override async Task<long> GetNext(MangosConditionEntryIdType request, int count, IdGenerationContext context)
        {
            var dbMax = (long)await databaseProvider.GetMaxConditionEntry();
            return Math.Max(Math.Max(dbMax, request.LocalMax), context.MaxUsedId ?? 0) + 1;
        }
    }
}
