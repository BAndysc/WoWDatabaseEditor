using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.QuestChainEditor.QueryGenerators;

namespace WDE.QuestChainEditor.Test.QueryGenerators;

public class TrinityQueryGeneratorTest
{
    TrinitySqlGenerator generator;

    [SetUp]
    public void Setup()
    {
        generator = new TrinitySqlGenerator(Substitute.For<IDatabaseProvider>());
    }
    
    [Test]
    public void GenerateQuery_ShouldReturnCorrectQuery()
    {
        var query = generator.GenerateQuery(new List<ChainRawData>()
        {
            new ChainRawData(1, 2, 3, 4, 5)
        }, null).QueryString;
        
        Assert.AreEqual(" -- \nUPDATE `quest_template_addon` SET `PrevQuestID` = 2, `NextQuestID` = 3, `ExclusiveGroup` = 4, `BreadcrumbForQuestId` = 0 WHERE `ID` = 1;", query);
    }
    
    [Test]
    public void GenerateQuery_ShouldReturnCorrectQuery_OnlyPrevQuest()
    {
        var query = generator.GenerateQuery(new List<ChainRawData>()
        {
            new ChainRawData(1, 2, 3, 4, 5)
        }, new Dictionary<uint, ChainRawData>()
        {
            {1, new(1, 0, 3, 4, 5)}
        }).QueryString;
        
        Assert.AreEqual(" -- \nUPDATE `quest_template_addon` SET `PrevQuestID` = 2 WHERE `ID` = 1;", query);
    }
    
    [Test]
    public void GenerateQuery_ShouldReturnCorrectQuery_OnlyNextQuest()
    {
        var query = generator.GenerateQuery(new List<ChainRawData>()
        {
            new ChainRawData(1, 2, 3, 4, 5)
        }, new Dictionary<uint, ChainRawData>()
        {
            {1, new(1, 2, 0, 4, 5)}
        }).QueryString;
        
        Assert.AreEqual(" -- \nUPDATE `quest_template_addon` SET `NextQuestID` = 3 WHERE `ID` = 1;", query);
    }
    
    [Test]
    public void GenerateQuery_ShouldReturnCorrectQuery_OnlyExclusiveGroup()
    {
        var query = generator.GenerateQuery(new List<ChainRawData>()
        {
            new ChainRawData(1, 2, 3, 4, 5)
        }, new Dictionary<uint, ChainRawData>()
        {
            {1, new(1, 2, 3, 0, 5)}
        }).QueryString;
        
        Assert.AreEqual(" -- \nUPDATE `quest_template_addon` SET `ExclusiveGroup` = 4 WHERE `ID` = 1;", query);
    }
    
    [Test]
    public void GenerateQuery_NoChange()
    {
        var query = generator.GenerateQuery(new List<ChainRawData>()
        {
            new ChainRawData(1, 2, 3, 4, 5),
            new ChainRawData(2, 0 ,0, 0,0),
        }, new Dictionary<uint, ChainRawData>()
        {
            {1, new(1, 2, 3, 4, 5)},
            {2, new(2, 0, 0, 0, 0)},
        }).QueryString;
        
        Assert.AreEqual("", query);
    }
}