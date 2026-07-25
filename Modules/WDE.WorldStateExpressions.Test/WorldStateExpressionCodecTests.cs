using System.Linq;
using NUnit.Framework;
using WDE.WorldStateExpressions.Models;

namespace WDE.WorldStateExpressions.Test
{
    public class WorldStateExpressionCodecTests
    {
        // real rows from the cmangos worldstate_expression table (client WorldStateExpression.db2 dump)
        [TestCase("01023F0000000003023E0000000000")] // id 44: WS[63] < WS[62]
        [TestCase("0103010000000132000000014B000000000000")] // id 61: Random(50, 75)
        [TestCase("01023F0000000005023E00000000010229000000000301020000000000")] // id 82: two clauses AND
        [TestCase("0102C900000000010100000000000202CA00000000010100000000000202CB0000000001010000000000")] // id 261: three clauses OR, no trailing terminator
        [TestCase("0102650000000001010100000000020269010000000101010000000000")] // id 125
        [TestCase("01030100000001010000000106000000000000")] // id 281: Random(1, 6)
        public void RoundTrip_Is_ByteExact(string hex)
        {
            var expression = WorldStateExpressionCodec.Decode(hex);
            Assert.AreEqual(hex, WorldStateExpressionCodec.Encode(expression));
        }

        [Test]
        public void Decodes_SingleClause_WorldStateComparison()
        {
            var expression = WorldStateExpressionCodec.Decode("01023F0000000003023E0000000000");

            Assert.IsTrue(expression.Enabled);
            Assert.IsTrue(expression.HasTrailingTerminator);
            Assert.AreEqual(1, expression.Clauses.Count);

            var clause = expression.Clauses[0];
            Assert.AreEqual(WseCompare.LessThan, clause.Compare);
            Assert.AreEqual(new WseWorldState(63), clause.Left.Left);
            Assert.AreEqual(new WseWorldState(62), clause.Right!.Left);
        }

        [Test]
        public void Decodes_MultiClause_Logic()
        {
            var expression = WorldStateExpressionCodec.Decode(
                "0102C900000000010100000000000202CA00000000010100000000000202CB0000000001010000000000");

            Assert.AreEqual(3, expression.Clauses.Count);
            Assert.AreEqual(new[] { WseLogic.Or, WseLogic.Or }, expression.Logic);
            Assert.IsFalse(expression.HasTrailingTerminator);
            Assert.AreEqual(new WseWorldState(201), expression.Clauses[0].Left.Left);
            Assert.AreEqual(WseCompare.EqualTo, expression.Clauses[0].Compare);
            Assert.AreEqual(new WseConstant(0), expression.Clauses[0].Right!.Left);
        }

        [Test]
        public void Decodes_Function_With_Constant_Args()
        {
            var expression = WorldStateExpressionCodec.Decode("0103010000000132000000014B000000000000");

            var function = (WseFunctionValue)expression.Clauses[0].Left.Left;
            Assert.AreEqual(1u, function.FunctionId); // Random
            Assert.AreEqual(new WseConstant(50), function.Arg1);
            Assert.AreEqual(new WseConstant(75), function.Arg2);
            Assert.AreEqual(WseCompare.None, expression.Clauses[0].Compare);
        }

        [Test]
        public void Nested_Function_Args_RoundTrip()
        {
            var expression = new WseExpression();
            expression.Clauses.Add(new WseClause(new WseValue(
                new WseFunctionValue(1, new WseConstant(1),
                    new WseFunctionValue(6, new WseConstant(0), new WseConstant(0))))));

            var hex = WorldStateExpressionCodec.Encode(expression);
            var decoded = WorldStateExpressionCodec.Decode(hex);
            var function = (WseFunctionValue)decoded.Clauses[0].Left.Left;
            var nested = (WseFunctionValue)function.Arg2;
            Assert.AreEqual(6u, nested.FunctionId); // ClockHour
            Assert.AreEqual("Random(1, ClockHour())", WorldStateExpressionCodec.ToReadable(decoded));
        }

        [Test]
        public void Readable_SingleClause()
        {
            var expression = WorldStateExpressionCodec.Decode("01023F0000000003023E0000000000");
            Assert.AreEqual("WS[63] < WS[62]", WorldStateExpressionCodec.ToReadable(expression));
        }

        [Test]
        public void Readable_MultiClause_Wraps_In_Parens()
        {
            var expression = WorldStateExpressionCodec.Decode(
                "01023F0000000005023E00000000010229000000000301020000000000");
            Assert.AreEqual("(WS[63] > WS[62]) AND (WS[41] < 2)", WorldStateExpressionCodec.ToReadable(expression));
        }

        [Test]
        public void Readable_Uniform_Logic_Stays_Flat()
        {
            // id 261: WS[201]=0 OR WS[202]=0 OR WS[203]=0 — associative, no fold parens needed
            var expression = WorldStateExpressionCodec.Decode(
                "0102C900000000010100000000000202CA00000000010100000000000202CB0000000001010000000000");
            Assert.AreEqual("(WS[201] = 0) OR (WS[202] = 0) OR (WS[203] = 0)",
                WorldStateExpressionCodec.ToReadable(expression));
        }

        [Test]
        public void Readable_Mixed_Logic_Makes_Left_Fold_Explicit()
        {
            // A OR B AND C evaluates as (A OR B) AND C — no precedence in the format
            var expression = new WseExpression();
            expression.Clauses.Add(new WseClause(new WseValue(new WseWorldState(1))));
            expression.Clauses.Add(new WseClause(new WseValue(new WseWorldState(2))));
            expression.Clauses.Add(new WseClause(new WseValue(new WseWorldState(3))));
            expression.Logic.Add(WseLogic.Or);
            expression.Logic.Add(WseLogic.And);

            Assert.AreEqual("((WS[1]) OR (WS[2])) AND (WS[3])",
                WorldStateExpressionCodec.ToReadable(expression));

            expression.Clauses.Add(new WseClause(new WseValue(new WseWorldState(4))));
            expression.Logic.Add(WseLogic.Xor);
            Assert.AreEqual("(((WS[1]) OR (WS[2])) AND (WS[3])) XOR (WS[4])",
                WorldStateExpressionCodec.ToReadable(expression));
        }

        [Test]
        public void Readable_Function_Trims_Trailing_Zero_Args()
        {
            var expression = WorldStateExpressionCodec.Decode("0103010000000132000000014B000000000000");
            Assert.AreEqual("Random(50, 75)", WorldStateExpressionCodec.ToReadable(expression));
        }

        [Test]
        public void Readable_Uses_WorldState_Names_When_Resolver_Provided()
        {
            var expression = WorldStateExpressionCodec.Decode("01023F0000000003023E0000000000");
            string? Resolver(long id) => id == 63 ? "TOWER COUNT" : null;
            Assert.AreEqual("WS[TOWER COUNT (63)] < WS[62]",
                WorldStateExpressionCodec.ToReadable(expression, Resolver));
        }

        [Test]
        public void Readable_Disabled_Expression()
        {
            var expression = WorldStateExpressionCodec.Decode("00023F0000000003023E0000000000");
            Assert.AreEqual("[disabled] WS[63] < WS[62]", WorldStateExpressionCodec.ToReadable(expression));
        }

        [Test]
        public void Encode_Builds_Expression_From_Scratch()
        {
            var expression = new WseExpression();
            expression.Clauses.Add(new WseClause(
                new WseValue(new WseWorldState(63)),
                WseCompare.LessThan,
                new WseValue(new WseWorldState(62))));

            Assert.AreEqual("01023F0000000003023E0000000000", WorldStateExpressionCodec.Encode(expression));
        }

        [Test]
        public void Encode_Arithmetic_And_Negative_Constant()
        {
            var expression = new WseExpression();
            expression.Clauses.Add(new WseClause(
                new WseValue(new WseWorldState(5), WseOperator.Add, new WseConstant(-1)),
                WseCompare.GreaterThan,
                new WseValue(new WseConstant(10))));

            var roundTripped = WorldStateExpressionCodec.Decode(WorldStateExpressionCodec.Encode(expression));
            var clause = roundTripped.Clauses[0];
            Assert.AreEqual(WseOperator.Add, clause.Left.Operator);
            Assert.AreEqual(new WseConstant(-1), clause.Left.Right);
            Assert.AreEqual("WS[5] + -1 > 10", WorldStateExpressionCodec.ToReadable(roundTripped));
        }

        [TestCase("")]
        [TestCase("0")] // odd length
        [TestCase("XY")] // not hex
        [TestCase("01")] // truncated: no clause
        [TestCase("0102")] // truncated worldstate id
        [TestCase("01023F00000000")] // truncated: missing compare
        [TestCase("01023F0000000003023E000000000000FF")] // garbage after terminator
        [TestCase("010A3F0000000003023E0000000000")] // unknown value type 10
        public void Malformed_Input_Throws(string hex)
        {
            Assert.Throws<WseParseException>(() => WorldStateExpressionCodec.Decode(hex));
        }

        [Test]
        public void TryDecode_Returns_False_On_Malformed()
        {
            Assert.IsFalse(WorldStateExpressionCodec.TryDecode("ZZ", out _, out var error));
            Assert.IsNotEmpty(error);
            Assert.IsTrue(WorldStateExpressionCodec.TryDecode("01023F0000000003023E0000000000", out _, out _));
        }

        [Test]
        public void Whole_Shipped_Table_RoundTrips()
        {
            // a broader sample of shipped rows with various shapes
            var samples = new[]
            {
                "01023F0000000101010000000000",
                "010229000000000401000000000000",
                "0102650000000001010100000000020269010000000101010000000000",
                "01028D0000000101010000000000",
                "0102960000000001010000000000010297000000000101000000000000",
                "010205010000000101010000000000",
            };
            foreach (var hex in samples)
                Assert.AreEqual(hex, WorldStateExpressionCodec.Encode(WorldStateExpressionCodec.Decode(hex)), hex);
        }
    }
}
