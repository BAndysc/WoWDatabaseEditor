using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DbEditorsSolutionItemNameProvider : ISolutionNameProvider<DbEditorsSolutionItem>
    {
        public string GetName(DbEditorsSolutionItem item) => item.TableData.TableDescription;
    }
}