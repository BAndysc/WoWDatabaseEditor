using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
    [Table(Name = "game_event")]
    public class GameEventWoTLK : IGameEvent
    {
        /// <summary>
        /// Entry of the game event
        /// </summary>
        [Column("entry", IsPrimaryKey = true)] public ushort Entry { get; set; } // mediumint(8) unsigned
        [Column("schedule_type")] public int ScheduleType { get; set; } // int(11)
        /// <summary>
        /// Delay in minutes between occurences of the event
        /// </summary>
        [Column("occurence")] public ulong Occurence { get; set; } // bigint(20) unsigned
        /// <summary>
        /// Length in minutes of the event
        /// </summary>
        [Column("length")] public ulong Length { get; set; } // bigint(20) unsigned
        /// <summary>
        /// Client side holiday id
        /// </summary>
        [Column("holiday")] public uint Holiday { get; set; } // mediumint(8) unsigned
        /// <summary>
        /// This event starts only if defined LinkedTo event is started
        /// </summary>
        [Column("linkedTo")] public uint LinkedTo { get; set; } // mediumint(8) unsigned
        //[Column("EventGroup")] public uint EventGroup { get; set; } // mediumint(8) unsigned
        /// <summary>
        /// Description of the event displayed in console
        /// </summary>
        [Column("description")] public string? Description { get; set; } // varchar(255)
    }
}