using System;
using System.Linq.Expressions;
using NUnit.Framework;
using WDE.Common.Database;

namespace WDE.SqlQueryGenerator.Test
{
    public class DeleteTests
    {
        public void Test(string? condition, Expression<Func<IRow, bool>> predicate)
        {
            var query = Queries.Table(DatabaseTable.WorldTable("abc")).Where(predicate).Delete().QueryString;
            if (condition == null)
                Assert.AreEqual("DELETE FROM `abc`;", query);
            else
                Assert.AreEqual($"DELETE FROM `abc` WHERE {condition};", query);
        }
        
        [Test]
        public void TestDeleteNoCondition()
        {
            Test(null, r => true);
        }
        
        [Test]
        public void TestDeleteSimpleConditionInt() => Test("`Id` = 123", r => r.Column<int>("Id") == 123);
        
        [Test]
        public void TestDeleteSimpleConditionBool() => Test("`Id` = 1", r => r.Column<bool>("Id") == true);
        
        [Test]
        public void TestDeleteSimpleConditionBoolImplicit() => Test("`Id` = 1", r => r.Column<bool>("Id"));
        
        [Test]
        public void TestDeleteSimpleConditionBoolFalse() => Test("`Id` = 0", r => r.Column<bool>("Id") == false);
        
        [Test]
        public void TestDeleteSimpleConditionBoolFalseReversed() => Test("0 = `Id`", r => false == r.Column<bool>("Id"));
        
        [Test]
        public void TestDeleteSimpleConditionBoolFalseImplicit() => Test("`Id` != 1", r => !r.Column<bool>("Id"));
        
        [Test]
        public void TestDeleteSimpleConditionUint() => Test("`Id` = 123", r => r.Column<uint>("Id") == (uint)123);
        
        [Test]
        public void TestDeleteSimpleConditionLong() => Test("`Id` = 123", r => r.Column<long>("Id") == (long)123);
        
        [Test]
        public void TestDeleteSimpleConditionUlong() => Test("`Id` = 123", r => r.Column<ulong>("Id") == (ulong)123);
        
        [Test]
        public void TestDeleteSimpleConditionByte() => Test("`Id` = 123", r => r.Column<byte>("Id") == (byte)123);
        
        [Test]
        public void TestDeleteSimpleConditionSByte() => Test("`Id` = 123", r => r.Column<sbyte>("Id") == (sbyte)123);
        
        [Test]
        public void TestDeleteSimpleConditionShort() => Test("`Id` = 123", r => r.Column<short>("Id") == (short)123);
        
        [Test]
        public void TestDeleteSimpleConditionUshort() => Test("`Id` = 123", r => r.Column<ushort>("Id") == (ushort)123);
        
        [Test]
        public void TestDeleteSimpleConditionString() => Test("`Id` = 'abc \"2\"'", r => r.Column<string>("Id") == "abc \"2\"");
        
        [Test]
        public void TestDeleteSimpleConditionFloat() => Test("`Id` = 2.5", r => r.Column<float>("Id") == 2.5f);
        
        [Test]
        public void TestDeleteSimpleConditionDouble() => Test("`Id` = 2.5", r => r.Column<double>("Id") == 2.5);
        
        // Conversion
        [Test]
        public void TestDeleteSimpleConditionIntConvertUint() => Test("`Id` = 3", r => r.Column<int>("Id") == (uint)3);
        
        [Test]
        public void TestDeleteSimpleConditionUIntConvertInt() => Test("`Id` = -3", r => r.Column<int>("Id") == -3);
        
        // Simple OR AND
        [Test]
        public void TestDeleteSimpleOr() => Test("`Id` = 1 OR `Id` = 2", r => r.Column<uint>("Id") == 1 || r.Column<int>("Id") == 2);
        
        [Test]
        public void TestDeleteSimpleAnd() => Test("`Id` = 1 AND `Id` = 2", r => r.Column<uint>("Id") == 1 && r.Column<int>("Id") == 2);
        
        [Test]
        public void TestDeleteSimpleOrAnd() => Test("(`Id` = 1 OR `Id` = 2) AND `Ab` != 10", r => (r.Column<uint>("Id") == 1 || r.Column<int>("Id") == 2) && r.Column<int>("Ab") != 10);
        
        // Test simplify
        [Test]
        public void TestDeleteSimplify()
        {
            bool? val = null;
            Test(null, r => !val.HasValue || r.Column<bool>("Id") == val.Value);
            Test(null, r => r.Column<bool>("Id") == val!.Value || !val.HasValue);
            
            val = true;
            Test("`Id` = 1", r => !val.HasValue || r.Column<bool>("Id") == val.Value);
            Test("`Id` = 1", r => r.Column<bool>("Id") == val.Value || !val.HasValue);
            
            val = false;
            Test("`Id` = 0", r => !val.HasValue || r.Column<bool>("Id") == val.Value);
            Test("`Id` = 0", r => r.Column<bool>("Id") == val.Value || !val.HasValue);
        }

        public static bool True = true;
        
        [Test]
        public void TestDeleteSimplifyOrElse() => Test(null, r => True || True);

        [Test]
        public void TestDeleteSimplifyAndAlso() => Test(null, r => True && True);

        [Test]
        public void TestDeleteSimplifyAndAlsoLeft() => Test("`a` = 1", r => r.Column<bool>("a") && True);

        [Test]
        public void TestDeleteSimplifyAndAlsoRight() => Test("`a` = 1", r => True && r.Column<bool>("a"));

        // Compound expression
        [Test]
        public void TestCompoundExpression()
        {
            Test("`Entry` = 1200 AND (`x` > 10 AND `x` < 20 OR `y` > 20 AND `y` < 30) OR `Entry` = 5120 AND `x` > -10 AND `x` < -5 AND `y` > 20 AND `y` < 30",
                r => r.Column<int>("Entry") == 1200 && 
                        ((r.Column<float>("x") > 10 && r.Column<float>("x") < 20) || 
                         (r.Column<float>("y") > 20 && r.Column<float>("y") < 30)) ||
                        r.Column<int>("Entry") == 5120 && 
                        ((r.Column<float>("x") > -10 && r.Column<float>("x") < -5) && 
                         (r.Column<float>("y") > 20 && r.Column<float>("y") < 30)));
        }

        // Precedence test
        [Test]
        public void TestPrecedence()
        {
            Test("~(1 * 3 / (2 + (1 << `Id`))) > 1", r => ~(1 * (3 / (2 + (1 << r.Column<int>("Id"))))) > 1);
        }
        
        [Test]
        public void TestPrecedenceBool()
        {
            Test("`b` = 1 AND (`a` = 1 OR 2 > (3 & `d`) ^ (1 | `c`))", r => r.Column<bool>("b") && (r.Column<bool>("a") || (2 > ((3 & r.Column<int>("d")) ^ (1 | r.Column<int>("c"))))));
        }
        
        // Test compare operators
        [Test]
        public void TestDeleteGreater() => Test("`a` > 2", r => r.Column<int>("a") > 2);
        
        [Test]
        public void TestDeleteGreaterThan() => Test("`a` >= 2", r => r.Column<int>("a") >= 2);
        
        [Test]
        public void TestDeleteLess() => Test("`a` < 2", r => r.Column<int>("a") < 2);
        
        [Test]
        public void TestDeleteLessThan() => Test("`a` <= 2", r => r.Column<int>("a") <= 2);
        
        [Test]
        public void TestDeleteEquals() => Test("`a` = 2", r => r.Column<int>("a") == 2);
        
        [Test]
        public void TestDeleteNotEquals() => Test("`a` != 2", r => r.Column<int>("a") != 2);

        [Test]
        public void TestDeleteNot() => Test("NOT (`a` > 2)", r => !(r.Column<int>("a") > 2));

        // variables
        [Test]
        public void TestDeleteVariable() => Test("`a` != @ENTRY", r => r.Column<int>("a") != r.Variable<int>("ENTRY"));
        
        // outer variable
        private int variable = 10;

        [Test]
        public void TestDeleteClassField() => Test("`a` = 10", r => r.Column<int>("a") == variable);
        
        private int Variable { get; } = 10;

        [Test]
        public void TestDeleteClassProperty() => Test("`a` = 10", r => r.Column<int>("a") == Variable);

        private InnerClass inner = new();

        [Test]
        public void TestCompoundClassProperty() => Test("`a` = 10", r => r.Column<int>("a") == inner.Inner.Int);

        private class InnerClass
        {
            public EvenMoreInnerClass Inner = new();
        }

        private class EvenMoreInnerClass
        {
            public int Int = 10;
        }
        
        [Test]
        public void TestDeleteMemberAccess()
        {
            string str = "abc";
            Test("`abc` = 10", r => r.Column<int>(str) == 10);
        }
        
        // where in

        [Test]
        public void TestWhereIn()
        {
            Assert.AreEqual("DELETE FROM `abc` WHERE `a` IN (1, 2);", Queries.Table(DatabaseTable.WorldTable("abc")).WhereIn("a", new int[]{1, 2}).Delete().QueryString);
        }

        [Test]
        public void TestWhereInAfterWhere()
        {
            var query = Queries
                .Table(DatabaseTable.WorldTable("abc"))
                .Where(r => r.Column<int>("c") == 1)
                .WhereIn("a", new int[] { 1, 2 })
                .Delete()
                .QueryString;

            var allowedA = "DELETE FROM `abc` WHERE (`c` = 1) AND (`a` IN (1, 2));";
            var allowedB = "DELETE FROM `abc` WHERE `c` = 1 AND `a` IN (1, 2);";
            
            if (query != allowedA && query != allowedB)
                Assert.AreEqual(allowedA, query);
            else if (query == allowedA)
                Assert.AreEqual(allowedA, query);
            else if (query == allowedB)
                Assert.AreEqual(allowedB, query);
        }
        
        [Test]
        public void TestWhereInAfterWhere2()
        {
            var query = Queries
                .Table(DatabaseTable.WorldTable("abc"))
                .Where(r => r.Column<int>("c") == 1 || r.Column<int>("c") == 2)
                .WhereIn("a", new int[] { 1, 2 })
                .Delete()
                .QueryString;
            
            var allowedA = "DELETE FROM `abc` WHERE (`c` = 1 OR `c` = 2) AND (`a` IN (1, 2));";
            var allowedB = "DELETE FROM `abc` WHERE (`c` = 1 OR `c` = 2) AND `a` IN (1, 2);";
            
            if (query != allowedA && query != allowedB)
                Assert.AreEqual(allowedA, query);
            else if (query == allowedA)
                Assert.AreEqual(allowedA, query);
            else if (query == allowedB)
                Assert.AreEqual(allowedB, query);
        }
    }
}