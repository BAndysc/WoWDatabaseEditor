namespace WDE.Common.Database
{
    /// <summary>
    /// One <c>creature_linking</c> row (CMaNGOS creature linking, guid-based variant): a specific
    /// slave spawn reacts to its master spawn's aggro/evade/death/respawn events. The slave guid is
    /// the table's primary key - a spawn is slave to at most one master.
    /// </summary>
    public interface ICreatureLinking
    {
        uint Guid { get; }
        uint MasterGuid { get; }
        uint Flag { get; }
    }

    /// <summary>
    /// One <c>creature_linking_template</c> row (CMaNGOS creature linking, entry-based variant):
    /// every spawn of the entry on the map is slave to the nearest master-entry spawn within
    /// <see cref="SearchRange"/> (0 = whole map, the master entry must then have exactly one spawn).
    /// Primary key is (entry, map).
    /// </summary>
    public interface ICreatureLinkingTemplate
    {
        uint Entry { get; }
        uint Map { get; }
        uint MasterEntry { get; }
        uint Flag { get; }
        uint SearchRange { get; }
    }

    public class AbstractCreatureLinking : ICreatureLinking
    {
        public uint Guid { get; set; }
        public uint MasterGuid { get; set; }
        public uint Flag { get; set; }
    }

    public class AbstractCreatureLinkingTemplate : ICreatureLinkingTemplate
    {
        public uint Entry { get; set; }
        public uint Map { get; set; }
        public uint MasterEntry { get; set; }
        public uint Flag { get; set; }
        public uint SearchRange { get; set; }
    }
}
