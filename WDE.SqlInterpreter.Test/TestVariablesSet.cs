using System.Linq;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.Common.Services.QueryParser.Models;

namespace WDE.SqlInterpreter.Test;

public class TestVariablesSet
{
    private BaseQueryEvaluator evaluator = null!;
    
    [SetUp]
    public void Setup()
    {
        evaluator = new("world", "hotfix", DataDatabaseType.World);
    }

    [Test]
    public void SimpleVariable()
    {
        var result = evaluator.ExtractInserts(
            "SET @ENTRY = 25123;\nINSERT INTO `smart_script` (`entryorguid`) VALUES\n(@ENTRY);\n").ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(DatabaseTable.WorldTable("smart_script"), result[0].TableName);
        Assert.AreEqual("entryorguid", result[0].Columns[0]);
        Assert.AreEqual(25123L, result[0].Inserts[0][0]);
    }

    [Test]
    public void MultiVariable()
    {
        var result = evaluator.ExtractInserts(
            "SET @ENTRY = 25123, @ENTRY2 = 3;\nINSERT INTO `smart_script` (`entryorguid`, abc) VALUES\n(@ENTRY2, @ENTRY);\n").ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(DatabaseTable.WorldTable("smart_script"), result[0].TableName);
        Assert.AreEqual("entryorguid", result[0].Columns[0]);
        Assert.AreEqual("abc", result[0].Columns[1]);
        Assert.AreEqual(3L, result[0].Inserts[0][0]);
        Assert.AreEqual(25123, result[0].Inserts[0][1]);
    }

    [Test]
    public void VariableInVariable()
    {
        var result = evaluator.ExtractUpdates(
            "SET @ENTRY = 2;\nSET @B = @ENTRY;\nUPDATE `smart_script` SET `entryorguid` = @B WHERE x=1;\n").ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(DatabaseTable.WorldTable("smart_script"), result[0].TableName);
        Assert.AreEqual("entryorguid", result[0].Updates[0].ColumnName);
        Assert.AreEqual(2L, result[0].Updates[0].Value);
    }

    [Test]
    public void CompoundExpression()
    {
        var result = evaluator.ExtractUpdates(
            "SET @ENTRY = (SELECT MAX(id) FROM creature);\nUPDATE `smart_script` SET `entryorguid` = @ENTRY WHERE x=1;\n").ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(DatabaseTable.WorldTable("smart_script"), result[0].TableName);
        Assert.AreEqual("entryorguid", result[0].Updates[0].ColumnName);
        Assert.AreEqual("(SELECT MAX(id) FROM creature)", ((UnknownSqlThing)result[0].Updates[0].Value!).Raw);
    }
}