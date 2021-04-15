using System;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Data;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DbEditorsSolutionItemNameProvider : ISolutionNameProvider<DbEditorsSolutionItem>
    {
        private readonly Lazy<IParameterFactory> parameterFactory;
        private readonly IDatabaseProvider databaseProvider;

        public DbEditorsSolutionItemNameProvider(Lazy<IParameterFactory> parameterFactory, IDatabaseProvider databaseProvider)
        {
            this.parameterFactory = parameterFactory;
            this.databaseProvider = databaseProvider;
        }
        
        public string GetName(DbEditorsSolutionItem item) => $"{item.TableId} ({item.Entry})";
    }
}