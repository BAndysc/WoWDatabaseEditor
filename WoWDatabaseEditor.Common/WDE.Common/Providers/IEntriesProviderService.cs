using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [UniqueProvider]
    public interface ICreatureEntryProviderService
    {
        uint? GetEntryFromService();
    }

    [UniqueProvider]
    public interface IGameobjectEntryProviderService
    {
        uint? GetEntryFromService();
    }

    [UniqueProvider]
    public interface IQuestEntryProviderService
    {
        uint? GetEntryFromService();
    }

    [UniqueProvider]
    public interface ISpellEntryProviderService
    {
        uint? GetEntryFromService();
    }
}
