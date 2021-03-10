using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Data
{
    public class DbTableFieldNameSwapHandler : IDbTableFieldNameSwapHandler
    {
        private readonly TableFieldsNameSwapDefinition definition;

        public DbTableFieldNameSwapHandler(string definitionPath)
        {
            definition = new();
        }

        public void SwapFieldName(IDbTableField field)
        {
            
        }
    }
}