using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.QueryUtils;

namespace WDE.SqlWorkbench.Test.Services.QueryUtils;

public class QueryUtilityTests
{
    private IQueryUtility util = new QueryUtility();
    private SimpleSelect select;

    [Test]
    public void Test_IsSimpleSelect()
    {
        Assert.IsTrue(util.IsSimpleSelect("SELECT * FROM `table`", out select));
        Assert.IsNull(select.From.Schema);
        Assert.AreEqual("table", select.From.Table);
        Assert.AreEqual("*", select.SelectItems);
        
        Assert.IsTrue(util.IsSimpleSelect("SELECT * FROM `table` WHERE 1", out select));
        Assert.IsNull(select.From.Schema);
        Assert.AreEqual("table", select.From.Table);
        Assert.AreEqual("*", select.SelectItems);
        
        Assert.IsTrue(util.IsSimpleSelect("SELECT * FROM `table` WHERE 1 ORDER BY a, b, c", out select));
        Assert.IsNull(select.From.Schema);
        Assert.AreEqual("table", select.From.Table);
        Assert.AreEqual("*", select.SelectItems);
        
        Assert.IsTrue(util.IsSimpleSelect("SELECT * FROM `db`.`table`", out select));
        Assert.AreEqual("db", select.From.Schema);
        Assert.AreEqual("table", select.From.Table);
        Assert.AreEqual("*", select.SelectItems);
        
        Assert.IsTrue(util.IsSimpleSelect("SELECT * FROM db.`table`", out select));
        Assert.AreEqual("db", select.From.Schema);
        Assert.AreEqual("table", select.From.Table);
        Assert.AreEqual("*", select.SelectItems);
        
        Assert.IsTrue(util.IsSimpleSelect("SELECT * FROM `db`.table", out select));
        Assert.AreEqual("db", select.From.Schema);
        Assert.AreEqual("table", select.From.Table);
        Assert.AreEqual("*", select.SelectItems);
        
        Assert.IsTrue(util.IsSimpleSelect("SELECT * FROM tab", out select));
        Assert.AreEqual("tab", select.From.Table);
        Assert.AreEqual("*", select.SelectItems);
                
        Assert.IsTrue(util.IsSimpleSelect("SELECT *, id FROM `db`.`table`", out select));
        Assert.AreEqual("db", select.From.Schema);
        Assert.AreEqual("table", select.From.Table);
        Assert.AreEqual("*, id", select.SelectItems);
        
        Assert.IsTrue(util.IsSimpleSelect("SELECT *, id, `name` FROM `db`.`table`", out select));
        Assert.AreEqual("db", select.From.Schema);
        Assert.AreEqual("table", select.From.Table);
        Assert.AreEqual("*, id, `name`", select.SelectItems);
    }

    [Test]
    public void Test_IsSimpleSelect_Aliasing_Fails()
    {
      Assert.IsFalse(util.IsSimpleSelect("SELECT * FROM `table` AS c", out select));
        
      Assert.IsFalse(util.IsSimpleSelect("SELECT a AS b FROM `table`", out select));
    }
    
    [Test]
    public void Test_IsSimpleSelect_GroupBy_Fails()
    {
      Assert.IsFalse(util.IsSimpleSelect("SELECT * FROM `table` GROUP BY a", out select));
    }
    
    [Test]
    public void Test_IsSimpleSelect_Having_Fails()
    {
      Assert.IsFalse(util.IsSimpleSelect("SELECT * FROM `table` HAVING a", out select));
    }
    
    [Test]
    public void Test_IsSimpleSelect_Union_Fails()
    {
      Assert.IsFalse(util.IsSimpleSelect("SELECT * FROM `table` UNION SELECT * FROM B", out select));
    }
}