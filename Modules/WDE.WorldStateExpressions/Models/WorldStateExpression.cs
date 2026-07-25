using System;
using System.Collections.Generic;

namespace WDE.WorldStateExpressions.Models
{
    public enum WseValueType : byte
    {
        Constant = 1,
        WorldState = 2,
        Function = 3
    }

    public enum WseOperator : byte
    {
        None = 0,
        Add = 1,
        Subtract = 2,
        Multiply = 3,
        Divide = 4,
        Remainder = 5
    }

    public enum WseCompare : byte
    {
        None = 0,
        EqualTo = 1,
        NotEqualTo = 2,
        LessThan = 3,
        LessThanOrEqualTo = 4,
        GreaterThan = 5,
        GreaterThanOrEqualTo = 6
    }

    public enum WseLogic : byte
    {
        None = 0,
        And = 1,
        Or = 2,
        Xor = 3
    }

    public abstract record WseSingleValue;

    public sealed record WseConstant(int Value) : WseSingleValue;

    public sealed record WseWorldState(uint WorldStateId) : WseSingleValue;

    public sealed record WseFunctionValue(uint FunctionId, WseSingleValue Arg1, WseSingleValue Arg2) : WseSingleValue;

    /// value with an optional arithmetic operator (EvalValue in the core)
    public sealed record WseValue(WseSingleValue Left, WseOperator Operator = WseOperator.None, WseSingleValue? Right = null);

    /// value with an optional comparison (EvalRelOp in the core); Compare == None means "truthy" (value != 0)
    public sealed record WseClause(WseValue Left, WseCompare Compare = WseCompare.None, WseValue? Right = null);

    public sealed class WseExpression
    {
        public bool Enabled { get; set; } = true;
        public List<WseClause> Clauses { get; } = new();
        /// Logic[i] joins Clauses[i] and Clauses[i + 1]
        public List<WseLogic> Logic { get; } = new();
        /// shipped data usually ends with an explicit logic-None byte, but not always (e.g. id 261);
        /// preserved so a decode → encode round trip is byte-exact
        public bool HasTrailingTerminator { get; set; } = true;
    }

    public class WseParseException : Exception
    {
        public WseParseException(string message) : base(message)
        {
        }
    }
}
