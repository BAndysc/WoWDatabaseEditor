using System.Collections.Generic;
using WDE.Common.Game;

namespace WDE.Common.Services.HeadConnector
{
    public readonly struct Ability
    {
        public uint SpellId { get; init; }
        public ICollection<MapDifficulty>? Modes { get; init; }
    }
}