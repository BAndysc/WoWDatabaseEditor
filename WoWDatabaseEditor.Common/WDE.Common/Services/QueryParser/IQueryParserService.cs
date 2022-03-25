using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Services.QueryParser.Models;
using WDE.Module.Attributes;

namespace WDE.Common.Services.QueryParser;

[UniqueProvider]
public interface IQueryParserService
{
    public Task<(IList<ISolutionItem> items, IList<string> errors)> GenerateItemsForQuery(string query);
}

public interface IQueryParsingContext
{
    void AddError(string error);
    void ProduceItem(ISolutionItem item);
}

[NonUniqueProvider]
public interface IQueryParserProvider
{
    public Task<bool> ParseDelete(DeleteQuery query, IQueryParsingContext context);
    public Task<bool> ParseInsert(InsertQuery query, IQueryParsingContext context);
    public Task<bool> ParseUpdate(UpdateQuery query, IQueryParsingContext context);
    public void Finish(IQueryParsingContext context);
}
