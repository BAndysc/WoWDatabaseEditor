using System;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.CustomCommands.Commands
{
    [AutoRegister]
    [SingleInstance]
    public class CreatureTextBroadcastId : IDatabaseTableCommand
    {
        private readonly IDatabaseProvider databaseProvider;
        public ImageUri Icon => new ImageUri("Icons/icon_discover.png");
        public string Name => "Fill BroadcastTextId";
        public string CommandId => "creatureTextToBroadcastId";

        public CreatureTextBroadcastId(IDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
        }
        
        public async Task Process(DatabaseCommandDefinitionJson definition, IDatabaseTableData tableData, DatabaseEntity? entityParameter, ITableContext addRow)
        {
            if (definition.Parameters == null || definition.Parameters.Length != 2)
                throw new Exception("Invalid command definition");

            var textColumn = new ColumnFullName(null, definition.Parameters[0]);
            var broadcastTextColumn = new ColumnFullName(null, definition.Parameters[1]);
            foreach (var entity in tableData.Entities)
            {
                var broadcastTextIdField = entity.GetCell(broadcastTextColumn) as DatabaseField<long>;
                var textField = entity.GetCell(textColumn) as DatabaseField<string>;

                if (textField == null || broadcastTextIdField == null)
                    throw new Exception("Invalid command parameters");

                if (textField.Current.Value == null)
                    continue;
                
                var broadcastText = await databaseProvider.GetBroadcastTextByTextAsync(textField.Current.Value);
                
                if (broadcastText != null)
                    broadcastTextIdField.Current.Value = broadcastText.Id;
            }
        }
    }
}