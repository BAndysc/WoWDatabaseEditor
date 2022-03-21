using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;
using WDE.Common.Services;
using WDE.Common.Sessions;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Services;
using WDE.DatabaseEditors.Solution;
using WDE.SqlInterpreter;

namespace WDE.DatabaseEditors.Test.Services;

public class QueryParserServiceTests
{
    private IQueryParserService parserService;

    [SetUp]
    public void Setup()
    {
        var tableDefinitionProvider = Substitute.For<ITableDefinitionProvider>();
        var sessionService = Substitute.For<ISessionService>();
        var dataLoader = Substitute.For<IDatabaseTableDataProvider>();

        var definition = new DatabaseTableDefinitionJson()
        {
            Id = "table",
            RecordMode = RecordMode.SingleRow,
            PrimaryKey = new List<string>() { "spawnId", "spawnType" },
            Groups = new List<DatabaseColumnsGroupJson>()
            {
                new DatabaseColumnsGroupJson()
                {
                    Fields = new List<DatabaseColumnJson>()
                    {
                        new()
                        {
                            DbColumnName = "spawnType"
                        },
                        new()
                        {
                            DbColumnName = "spawnId"
                        },
                        new()
                        {
                            DbColumnName = "a"
                        },
                        new()
                        {
                            DbColumnName = "b"
                        },
                        new()
                        {
                            DbColumnName = "c"
                        },
                    }
                }
            }
        };

        dataLoader.Load(default!, default, default, default, default)
            .ReturnsForAnyArgs(
                Task.FromResult<IDatabaseTableData>(new DatabaseTableData(definition, new List<DatabaseEntity>())));

        tableDefinitionProvider
            .GetDefinition("table")
            .Returns(definition);

        tableDefinitionProvider
            .GetDefinitionByTableName("table")
            .Returns(definition);


        parserService = new QueryParserService(
            tableDefinitionProvider,
            new QueryEvaluator(),
            sessionService,
            dataLoader
        );
    }

    [Test]
    public async Task TestInsertRow()
    {
        var result = await parserService.GenerateItemsForQuery(
            "INSERT IGNORE INTO `table` (`spawnType`, `spawnId`, `a`, `b`, `c`) VALUES (0, 18675, 1, 8, 0)");
        Assert.AreEqual(0, result.errors.Count);
        Assert.AreEqual(1, result.items.Count);
        var item = (DatabaseTableSolutionItem)result.items[0];
        Assert.AreEqual("table", item.DefinitionId);
        Assert.AreEqual(1, item.Entries.Count);
        Assert.AreEqual(new DatabaseKey(18675, 0), item.Entries[0].Key);
        Assert.AreEqual(false, item.Entries[0].ExistsInDatabase);
    }

    [Test]
    public async Task TestInsertRowDifferentOrderDoesntMatter()
    {
        var result = await parserService.GenerateItemsForQuery(
            "INSERT IGNORE INTO `table` (`spawnId`, `spawnType`, `a`, `b`, `c`) VALUES (18675, 0, 1, 8, 0)");
        Assert.AreEqual(0, result.errors.Count);
        Assert.AreEqual(1, result.items.Count);
        var item = (DatabaseTableSolutionItem)result.items[0];
        Assert.AreEqual("table", item.DefinitionId);
        Assert.AreEqual(1, item.Entries.Count);
        Assert.AreEqual(new DatabaseKey(18675, 0), item.Entries[0].Key);
        Assert.AreEqual(false, item.Entries[0].ExistsInDatabase);
    }

    [Test]
    public async Task TestInsertTwoRows()
    {
        var result = await parserService.GenerateItemsForQuery(
            "INSERT IGNORE INTO `table` (`spawnType`, `spawnId`, `a`, `b`, `c`) VALUES (0, 18675, 1, 8, 0), (1, 18675, 1, 8, 0)");
        Assert.AreEqual(0, result.errors.Count);
        Assert.AreEqual(1, result.items.Count);
        var item = (DatabaseTableSolutionItem)result.items[0];
        Assert.AreEqual("table", item.DefinitionId);
        Assert.AreEqual(2, item.Entries.Count);
        Assert.AreEqual(new DatabaseKey(18675, 0), item.Entries[0].Key);
        Assert.AreEqual(new DatabaseKey(18675, 1), item.Entries[1].Key);
        Assert.AreEqual(false, item.Entries[0].ExistsInDatabase);
        Assert.AreEqual(false, item.Entries[1].ExistsInDatabase);
    }

    [Test]
    public async Task TestDelete()
    {
        var result = await parserService.GenerateItemsForQuery(
            "DELETE FROM `table` WHERE `spawnType` = 0 AND `spawnId` = 18675;");
        Assert.AreEqual(0, result.errors.Count);
        Assert.AreEqual(1, result.items.Count);
        var item = (DatabaseTableSolutionItem)result.items[0];
        Assert.AreEqual("table", item.DefinitionId);
        Assert.AreEqual(0, item.Entries.Count);
        Assert.AreEqual(new DatabaseKey(18675, 0), item.DeletedEntries[0]);
    }
    
    [Test]
    public async Task TestDeleteThenInsert()
    {
        var result = await parserService.GenerateItemsForQuery(
            "DELETE FROM `table` WHERE `spawnType` = 0 AND `spawnId` = 18675; INSERT IGNORE INTO `table` (`spawnType`, `spawnId`, `a`, `b`, `c`) VALUES (0, 18675, 1, 8, 0);");
        Assert.AreEqual(0, result.errors.Count);
        Assert.AreEqual(1, result.items.Count);
        var item = (DatabaseTableSolutionItem)result.items[0];
        Assert.AreEqual("table", item.DefinitionId);
        Assert.AreEqual(0, item.DeletedEntries.Count);
        Assert.AreEqual(1, item.Entries.Count);
        Assert.AreEqual(new DatabaseKey(18675, 0), item.Entries[0].Key);
        Assert.AreEqual(false, item.Entries[0].ExistsInDatabase);
    }
}