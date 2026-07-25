using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WDE.WorldStateExpressions.Models
{
    /// Codec for the cmangos worldstate_expression.Expression column: a hex string holding
    /// the client WorldStateExpression byte code (little-endian, recursive descent grammar
    /// mirroring EvalRelOp/EvalValue/EvalSingleValue in mangos-wotlk WorldStateExpression.cpp).
    public static class WorldStateExpressionCodec
    {
        public static readonly IReadOnlyList<string> FunctionNames = new[]
        {
            "None", "Random", "Month", "Day", "TimeOfDay", "Region", "ClockHour", "OldDifficultyId",
            "HolidayStart", "HolidayLeft", "HolidayActive", "TimerCurrentTime", "WeekNumber", "Unk13",
            "Unk14", "DifficultyId", "WarModeActive", "Unk17", "Unk18", "Unk19", "Unk20", "Unk21",
            "WorldStateExpression", "KeystoneAffix", "Unk24", "Unk25", "Unk26", "Unk27", "KeystoneLevel",
            "Unk29", "Unk30", "Unk31", "Unk32", "MersenneRandom", "Unk34", "Unk35", "Unk36",
            "UiWidgetData", "TimeEventPassed"
        };

        public static string FunctionName(uint functionId) =>
            functionId < FunctionNames.Count ? FunctionNames[(int)functionId] : $"Function{functionId}";

        public static bool TryDecode(string hex, out WseExpression expression, out string error)
        {
            try
            {
                expression = Decode(hex);
                error = "";
                return true;
            }
            catch (WseParseException e)
            {
                expression = new WseExpression();
                error = e.Message;
                return false;
            }
        }

        public static WseExpression Decode(string hex)
        {
            var reader = new Reader(hex);
            var expression = new WseExpression
            {
                Enabled = reader.ReadByte() != 0
            };

            expression.Clauses.Add(ReadClause(reader));
            while (true)
            {
                if (reader.AtEnd)
                {
                    expression.HasTrailingTerminator = false;
                    break;
                }

                var logic = (WseLogic)reader.ReadByte();
                if (logic == WseLogic.None)
                {
                    if (!reader.AtEnd)
                        throw new WseParseException($"{reader.Remaining} unexpected byte(s) after the final logic terminator");
                    expression.HasTrailingTerminator = true;
                    break;
                }

                if (logic > WseLogic.Xor)
                    throw new WseParseException($"unknown clause logic {(int)logic}");

                expression.Logic.Add(logic);
                expression.Clauses.Add(ReadClause(reader));
            }

            return expression;
        }

        private static WseClause ReadClause(Reader reader)
        {
            var left = ReadValue(reader);
            var compare = (WseCompare)reader.ReadByte();
            if (compare == WseCompare.None)
                return new WseClause(left);
            if (compare > WseCompare.GreaterThanOrEqualTo)
                throw new WseParseException($"unknown comparison operator {(int)compare}");
            return new WseClause(left, compare, ReadValue(reader));
        }

        private static WseValue ReadValue(Reader reader)
        {
            var left = ReadSingleValue(reader);
            var op = (WseOperator)reader.ReadByte();
            if (op == WseOperator.None)
                return new WseValue(left);
            if (op > WseOperator.Remainder)
                throw new WseParseException($"unknown arithmetic operator {(int)op}");
            return new WseValue(left, op, ReadSingleValue(reader));
        }

        private static WseSingleValue ReadSingleValue(Reader reader)
        {
            var type = (WseValueType)reader.ReadByte();
            switch (type)
            {
                case WseValueType.Constant:
                    return new WseConstant(reader.ReadInt32());
                case WseValueType.WorldState:
                    return new WseWorldState(reader.ReadUInt32());
                case WseValueType.Function:
                {
                    var functionId = reader.ReadUInt32();
                    var arg1 = ReadSingleValue(reader);
                    var arg2 = ReadSingleValue(reader);
                    return new WseFunctionValue(functionId, arg1, arg2);
                }
                default:
                    throw new WseParseException($"unknown value type {(int)type}");
            }
        }

        public static string Encode(WseExpression expression)
        {
            if (expression.Clauses.Count == 0)
                throw new WseParseException("an expression must have at least one clause");
            if (expression.Logic.Count != expression.Clauses.Count - 1)
                throw new WseParseException("an expression with N clauses needs exactly N - 1 logic operators");

            var writer = new Writer();
            writer.WriteByte(expression.Enabled ? (byte)1 : (byte)0);
            WriteClause(writer, expression.Clauses[0]);
            for (int i = 1; i < expression.Clauses.Count; ++i)
            {
                if (expression.Logic[i - 1] == WseLogic.None)
                    throw new WseParseException("logic operator between clauses cannot be None");
                writer.WriteByte((byte)expression.Logic[i - 1]);
                WriteClause(writer, expression.Clauses[i]);
            }
            if (expression.HasTrailingTerminator)
                writer.WriteByte((byte)WseLogic.None);
            return writer.ToHex();
        }

        private static void WriteClause(Writer writer, WseClause clause)
        {
            WriteValue(writer, clause.Left);
            writer.WriteByte((byte)clause.Compare);
            if (clause.Compare != WseCompare.None)
                WriteValue(writer, clause.Right ?? new WseValue(new WseConstant(0)));
        }

        private static void WriteValue(Writer writer, WseValue value)
        {
            WriteSingleValue(writer, value.Left);
            writer.WriteByte((byte)value.Operator);
            if (value.Operator != WseOperator.None)
                WriteSingleValue(writer, value.Right ?? new WseConstant(0));
        }

        private static void WriteSingleValue(Writer writer, WseSingleValue value)
        {
            switch (value)
            {
                case WseConstant constant:
                    writer.WriteByte((byte)WseValueType.Constant);
                    writer.WriteInt32(constant.Value);
                    break;
                case WseWorldState worldState:
                    writer.WriteByte((byte)WseValueType.WorldState);
                    writer.WriteUInt32(worldState.WorldStateId);
                    break;
                case WseFunctionValue function:
                    writer.WriteByte((byte)WseValueType.Function);
                    writer.WriteUInt32(function.FunctionId);
                    WriteSingleValue(writer, function.Arg1);
                    WriteSingleValue(writer, function.Arg2);
                    break;
                default:
                    throw new WseParseException($"unknown single value {value.GetType().Name}");
            }
        }

        private static readonly string[] CompareSymbols = { "", "=", "≠", "<", "≤", ">", "≥" };
        private static readonly string[] OperatorSymbols = { "", "+", "-", "*", "/", "%" };
        private static readonly string[] LogicSymbols = { "", "AND", "OR", "XOR" };

        /// worldStateName resolves a worldstate id to its worldstate_name entry, when available
        public static string ToReadable(WseExpression expression, Func<long, string?>? worldStateName = null)
        {
            if (expression.Clauses.Count == 0)
                return "(empty)";

            var clauses = expression.Clauses.Select(clause =>
            {
                var sb = new StringBuilder();
                if (expression.Clauses.Count > 1)
                    sb.Append('(');
                AppendClause(sb, clause, worldStateName);
                if (expression.Clauses.Count > 1)
                    sb.Append(')');
                return sb.ToString();
            }).ToList();

            // clauses evaluate as a left-to-right fold with NO operator precedence
            // (A OR B AND C == (A OR B) AND C); with mixed operators make the fold
            // explicit with parentheses, uniform chains are associative so keep them flat
            var mixedLogic = expression.Logic.Distinct().Count() > 1;
            var result = clauses[0];
            for (int i = 1; i < clauses.Count; ++i)
            {
                if (mixedLogic && i > 1)
                    result = $"({result})";
                result = $"{result} {LogicSymbols[(int)expression.Logic[i - 1]]} {clauses[i]}";
            }

            return expression.Enabled ? result : $"[disabled] {result}";
        }

        private static void AppendClause(StringBuilder sb, WseClause clause, Func<long, string?>? worldStateName)
        {
            AppendValue(sb, clause.Left, worldStateName);
            if (clause.Compare == WseCompare.None)
                return;
            sb.Append(' ').Append(CompareSymbols[(int)clause.Compare]).Append(' ');
            AppendValue(sb, clause.Right ?? new WseValue(new WseConstant(0)), worldStateName);
        }

        private static void AppendValue(StringBuilder sb, WseValue value, Func<long, string?>? worldStateName)
        {
            AppendSingleValue(sb, value.Left, worldStateName);
            if (value.Operator == WseOperator.None)
                return;
            sb.Append(' ').Append(OperatorSymbols[(int)value.Operator]).Append(' ');
            AppendSingleValue(sb, value.Right ?? new WseConstant(0), worldStateName);
        }

        private static void AppendSingleValue(StringBuilder sb, WseSingleValue value, Func<long, string?>? worldStateName)
        {
            switch (value)
            {
                case WseConstant constant:
                    sb.Append(constant.Value.ToString(CultureInfo.InvariantCulture));
                    break;
                case WseWorldState worldState:
                    var name = worldStateName?.Invoke(worldState.WorldStateId);
                    if (name != null)
                        sb.Append("WS[").Append(name).Append(" (").Append(worldState.WorldStateId).Append(")]");
                    else
                        sb.Append("WS[").Append(worldState.WorldStateId).Append(']');
                    break;
                case WseFunctionValue function:
                    sb.Append(FunctionName(function.FunctionId)).Append('(');
                    // unused args are encoded as constant 0 — trim them from the tail for readability
                    var args = new[] { function.Arg1, function.Arg2 }.ToList();
                    while (args.Count > 0 && args[^1] is WseConstant { Value: 0 })
                        args.RemoveAt(args.Count - 1);
                    for (int i = 0; i < args.Count; ++i)
                    {
                        if (i > 0)
                            sb.Append(", ");
                        AppendSingleValue(sb, args[i], worldStateName);
                    }
                    sb.Append(')');
                    break;
            }
        }

        private class Reader
        {
            private readonly byte[] bytes;
            private int position;

            public Reader(string hex)
            {
                if (hex.Length == 0)
                    throw new WseParseException("expression is empty");
                if (hex.Length % 2 != 0)
                    throw new WseParseException("hex string has an odd number of characters");
                bytes = new byte[hex.Length / 2];
                for (int i = 0; i < bytes.Length; ++i)
                {
                    int high = HexDigit(hex[2 * i]);
                    int low = HexDigit(hex[2 * i + 1]);
                    bytes[i] = (byte)((high << 4) | low);
                }
            }

            private static int HexDigit(char c)
            {
                if (c >= '0' && c <= '9')
                    return c - '0';
                if (c >= 'A' && c <= 'F')
                    return c - 'A' + 10;
                if (c >= 'a' && c <= 'f')
                    return c - 'a' + 10;
                throw new WseParseException($"'{c}' is not a hex digit");
            }

            public bool AtEnd => position >= bytes.Length;
            public int Remaining => bytes.Length - position;

            public byte ReadByte()
            {
                if (position >= bytes.Length)
                    throw new WseParseException("unexpected end of expression");
                return bytes[position++];
            }

            public int ReadInt32() => unchecked((int)ReadUInt32());

            public uint ReadUInt32()
            {
                if (position + 4 > bytes.Length)
                    throw new WseParseException("unexpected end of expression");
                uint value = (uint)(bytes[position] | (bytes[position + 1] << 8) | (bytes[position + 2] << 16) | (bytes[position + 3] << 24));
                position += 4;
                return value;
            }
        }

        private class Writer
        {
            private readonly List<byte> bytes = new();

            public void WriteByte(byte b) => bytes.Add(b);

            public void WriteInt32(int value) => WriteUInt32(unchecked((uint)value));

            public void WriteUInt32(uint value)
            {
                bytes.Add((byte)(value & 0xFF));
                bytes.Add((byte)((value >> 8) & 0xFF));
                bytes.Add((byte)((value >> 16) & 0xFF));
                bytes.Add((byte)((value >> 24) & 0xFF));
            }

            public string ToHex()
            {
                var sb = new StringBuilder(bytes.Count * 2);
                foreach (var b in bytes)
                    sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }
    }
}
