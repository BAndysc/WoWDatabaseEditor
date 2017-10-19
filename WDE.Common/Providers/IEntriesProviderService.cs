using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common
{
    public interface ICreatureEntryProviderService
    {
        uint? GetEntryFromService();
    }

    public interface IGameobjectEntryProviderService
    {
        uint? GetEntryFromService();
    }

    public interface IQuestEntryProviderService
    {
        uint? GetEntryFromService();
    }

    public interface ISpellEntryProviderService
    {
        uint? GetEntryFromService();
    }
}
