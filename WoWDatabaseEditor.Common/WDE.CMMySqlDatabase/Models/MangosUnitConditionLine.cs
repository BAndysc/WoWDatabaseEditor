using System;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "unit_condition")]
public class MangosUnitConditionLine : IMangosUnitConditionLine
{
    [Column("Id")] public int Id { get; set; }
    [Column("Flags")] public uint Flags { get; set; }
    [Column("Variable_0")] public uint Variable0 { get; set; }
    [Column("Variable_1")] public uint Variable1 { get; set; }
    [Column("Variable_2")] public uint Variable2 { get; set; }
    [Column("Variable_3")] public uint Variable3 { get; set; }
    [Column("Variable_4")] public uint Variable4 { get; set; }
    [Column("Variable_5")] public uint Variable5 { get; set; }
    [Column("Variable_6")] public uint Variable6 { get; set; }
    [Column("Variable_7")] public uint Variable7 { get; set; }
    [Column("Op_0")] public uint Op0 { get; set; }
    [Column("Op_1")] public uint Op1 { get; set; }
    [Column("Op_2")] public uint Op2 { get; set; }
    [Column("Op_3")] public uint Op3 { get; set; }
    [Column("Op_4")] public uint Op4 { get; set; }
    [Column("Op_5")] public uint Op5 { get; set; }
    [Column("Op_6")] public uint Op6 { get; set; }
    [Column("Op_7")] public uint Op7 { get; set; }
    [Column("Value_0")] public int Value0 { get; set; }
    [Column("Value_1")] public int Value1 { get; set; }
    [Column("Value_2")] public int Value2 { get; set; }
    [Column("Value_3")] public int Value3 { get; set; }
    [Column("Value_4")] public int Value4 { get; set; }
    [Column("Value_5")] public int Value5 { get; set; }
    [Column("Value_6")] public int Value6 { get; set; }
    [Column("Value_7")] public int Value7 { get; set; }

    public UnitConditionClause GetClause(int index)
    {
        return index switch
        {
            0 => new UnitConditionClause { Variable = Variable0, Op = Op0, Value = Value0 },
            1 => new UnitConditionClause { Variable = Variable1, Op = Op1, Value = Value1 },
            2 => new UnitConditionClause { Variable = Variable2, Op = Op2, Value = Value2 },
            3 => new UnitConditionClause { Variable = Variable3, Op = Op3, Value = Value3 },
            4 => new UnitConditionClause { Variable = Variable4, Op = Op4, Value = Value4 },
            5 => new UnitConditionClause { Variable = Variable5, Op = Op5, Value = Value5 },
            6 => new UnitConditionClause { Variable = Variable6, Op = Op6, Value = Value6 },
            7 => new UnitConditionClause { Variable = Variable7, Op = Op7, Value = Value7 },
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }
}
