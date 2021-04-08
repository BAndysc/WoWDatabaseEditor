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
        
        public string GetName(DbEditorsSolutionItem item) => $"{GetItemDescriptionPrefix(item.TableContentType)} {GetNameOfItem(item.TableContentType, item.Entry)}";

        private string GetItemDescriptionPrefix(DbTableContentType contentType)
        {
            switch (contentType)
            {
                case DbTableContentType.CreatureTemplate:
                    return "Template of";
                case DbTableContentType.CreatureLootTemplate:
                    return "Creature loot of";
                case DbTableContentType.GameObjectTemplate:
                    return "Go Template of";
                default:
                    throw new Exception("[DbEditorsSolutionItemNameProvider] not defined table type!");
            }
        }

        private string GetNameOfItem(DbTableContentType contentType, uint entry)
        {
            switch (contentType)
            {
                case DbTableContentType.CreatureTemplate:
                case DbTableContentType.CreatureLootTemplate:
                    return databaseProvider.GetCreatureTemplate(entry)?.Name ?? $"creature {entry}";
                case DbTableContentType.GameObjectTemplate:
                    return databaseProvider.GetGameObjectTemplate(entry)?.Name ?? $"gameobject {entry}";
                default:
                    throw new Exception("[DbEditorsSolutionItemNameProvider] not defined table type!");
            }
        }
    }
}