namespace WDE.Common.Database
{
    /// <summary>
    /// One row of the cmangos `conditions` table. Logical types (-1 AND, -2 OR, -3 NOT)
    /// reference other condition entries via Value1..Value4.
    /// </summary>
    public interface IMangosConditionLine
    {
        /// <summary>Primary key; 0 = new row, not yet saved.</summary>
        uint ConditionEntry { get; }
        /// <summary>Signed: -3 NOT, -2 OR, -1 AND, 0..43 leaf condition types.</summary>
        int ConditionType { get; }
        uint Value1 { get; }
        uint Value2 { get; }
        uint Value3 { get; }
        uint Value4 { get; }
        /// <summary>0x1 = reverse result, 0x2 = swap source and target.</summary>
        uint Flags { get; }
        string? Comments { get; }
    }

    public class AbstractMangosConditionLine : IMangosConditionLine
    {
        public uint ConditionEntry { get; set; }
        public int ConditionType { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }
        public uint Value4 { get; set; }
        public uint Flags { get; set; }
        public string? Comments { get; set; }

        public AbstractMangosConditionLine() { }

        public AbstractMangosConditionLine(IMangosConditionLine other)
        {
            ConditionEntry = other.ConditionEntry;
            ConditionType = other.ConditionType;
            Value1 = other.Value1;
            Value2 = other.Value2;
            Value3 = other.Value3;
            Value4 = other.Value4;
            Flags = other.Flags;
            Comments = other.Comments;
        }
    }
}
