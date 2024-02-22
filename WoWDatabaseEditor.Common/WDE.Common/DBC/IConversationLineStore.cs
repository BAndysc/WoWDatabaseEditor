using System.Collections.Generic;
using WDE.Common.DBC.Structs;

namespace WDE.Common.DBC;

public interface IConversationLineStore
{
    IReadOnlyList<IConversationLine> ConversationLines { get; }
    IConversationLine? GetConversationLineById(uint id);
}