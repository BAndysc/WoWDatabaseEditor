using System;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.SQLEditor.ViewModels;
using WDE.SqlQueryGenerator;

namespace WDE.SQLEditor.Solutions
{
    [AutoRegister]
    [SingleInstance]
    public class Providers : ISolutionNameProvider<CustomSqlSolutionItem>, ISolutionItemIconProvider<CustomSqlSolutionItem>
    {
        public string GetName(CustomSqlSolutionItem item)
        {
            return item.Name;
        }

        public ImageUri GetIcon(CustomSqlSolutionItem icon)
        {
            return new ImageUri("Icons/document_sql.png");
        }
    }

    [AutoRegister]
    [SingleInstance]
    public class QueryGenerator : ISolutionItemSqlProvider<CustomSqlSolutionItem>
    {
        public Task<IQuery> GenerateSql(CustomSqlSolutionItem item)
        {
            return Task.FromResult(Queries.Raw(item.Database, item.Query));
        }
    }
    
    [AutoRegister]
    [SingleInstance]
    public class Serializer : ISolutionItemDeserializer<CustomSqlSolutionItem>, ISolutionItemSerializer<CustomSqlSolutionItem>
    {
        public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
        {
            solutionItem = null;
            if (projectItem.Type == 42 && projectItem.StringValue != null && projectItem.StringValue.Contains(":"))
            {
                var index = projectItem.StringValue.IndexOf(":");
                var id = projectItem.StringValue.Substring(0, index);
                var name = projectItem.StringValue.Substring(index + 1);
                solutionItem = new CustomSqlSolutionItem(id)
                {
                    Name = name,
                    Query = (projectItem.Comment ?? "").Replace("\\n", "\n")
                };
                return true;
            }

            return false;
        }

        public ISmartScriptProjectItem? Serialize(CustomSqlSolutionItem item, bool forMostRecentlyUsed)
        {
            return new AbstractSmartScriptProjectItem()
            {
                Type = 42,
                StringValue = $"{item.Id}:{item.Name}",
                Comment = item.Query.Replace("\n", "\\n")
            };
        }
    }
        
    [AutoRegister]
    [SingleInstance]
    public class EditorProvider : ISolutionItemEditorProvider<CustomSqlSolutionItem>
    {
        private readonly IContainerProvider containerProvider;

        public EditorProvider(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }
        
        public IDocument GetEditor(CustomSqlSolutionItem item)
        {
            return containerProvider.Resolve<CustomQueryEditorViewModel>((typeof(CustomSqlSolutionItem), item));
        }
    }

    //[AutoRegister]
    //[SingleInstance]
    public class ItemProvider : ISolutionItemProvider, INamedSolutionItemProvider
    {
        public string GetName()
        {
            return "Custom query document";
        }

        public ImageUri GetImage()
        {
            return new ImageUri("Icons/document_sql.png");
        }

        public string GetDescription()
        {
            return "Allows you to write and save absolutely any query";
        }

        public string GetGroupName()
        {
            return "Custom";
        }

        public bool IsCompatibleWithCore(ICoreVersion core)
        {
            return true;
        }

        public Task<ISolutionItem?> CreateSolutionItem()
        {
            throw new System.NotImplementedException();
        }

        public Task<ISolutionItem?> CreateSolutionItem(string name)
        {
            return Task.FromResult<ISolutionItem?>(new CustomSqlSolutionItem(Guid.NewGuid().ToString()){ Name = name});
        }
    }
}