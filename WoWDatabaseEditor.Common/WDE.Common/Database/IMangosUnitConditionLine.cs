using System;

namespace WDE.Common.Database
{
    /// <summary>
    /// One clause of a unit_condition row: runtimeValue(Variable, source, target) compared
    /// with Op against Value.
    /// </summary>
    public readonly struct UnitConditionClause
    {
        /// <summary>UnitCondition enum 0..86; 0 = NONE (clause unused, always true).</summary>
        public uint Variable { get; init; }
        /// <summary>ConditionOperation: 0 NONE (always true), 1 =, 2 !=, 3 &lt;, 4 &lt;=, 5 &gt;, 6 &gt;=.</summary>
        public uint Op { get; init; }
        public int Value { get; init; }

        public bool IsNone => Variable == 0;
    }

    /// <summary>
    /// One row of the cmangos `unit_condition` table: up to 8 clauses evaluated between
    /// a source and a target unit, combined with AND (Flags = 0) or OR (Flags &amp; 0x1).
    /// </summary>
    public interface IMangosUnitConditionLine
    {
        public const int ClausesCount = 8;

        /// <summary>Signed primary key. Positive ids are DBC-imported (wiped on reseed);
        /// negative ids are reserved for custom content. -1 is used by consumers as the
        /// "no condition" sentinel and must not be a row id.</summary>
        int Id { get; }

        /// <summary>Only bit 0x1 is used: 1 = OR the clauses, 0 = AND them.</summary>
        uint Flags { get; }

        UnitConditionClause GetClause(int index);
    }

    public class AbstractMangosUnitConditionLine : IMangosUnitConditionLine
    {
        public int Id { get; set; }
        public uint Flags { get; set; }
        public UnitConditionClause[] Clauses { get; } = new UnitConditionClause[IMangosUnitConditionLine.ClausesCount];

        public UnitConditionClause GetClause(int index) => Clauses[index];

        public void SetClause(int index, UnitConditionClause clause) => Clauses[index] = clause;

        public AbstractMangosUnitConditionLine() { }

        public AbstractMangosUnitConditionLine(IMangosUnitConditionLine other)
        {
            Id = other.Id;
            Flags = other.Flags;
            for (int i = 0; i < IMangosUnitConditionLine.ClausesCount; ++i)
                Clauses[i] = other.GetClause(i);
        }
    }
}
