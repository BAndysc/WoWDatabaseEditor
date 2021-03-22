using System;
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

        public DbEditorsSolutionItemNameProvider(Lazy<IParameterFactory> parameterFactory)
        {
            this.parameterFactory = parameterFactory;
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
                    var creatureParam = parameterFactory.Value.Factory("CreatureParameter");
                    if (creatureParam != null)
                        return creatureParam.ToString(entry);
                    else
                        return entry.ToString();
                case DbTableContentType.GameObjectTemplate:
                    var goParam = parameterFactory.Value.Factory("GameobjectParameter");
                    if (goParam != null)
                        return goParam.ToString(entry);
                    else
                        return entry.ToString();
                default:
                    throw new Exception("[DbEditorsSolutionItemNameProvider] not defined table type!");
            }
        }
    }
}