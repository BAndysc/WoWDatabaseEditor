using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Common
{
    //@todo: replace getters with properties
    [NonUniqueProvider]
    public interface ISolutionItemProvider
    {
        string GetName();
        ImageUri GetImage();
        string GetDescription();
        string GetGroupName();

        bool IsContainer => false;
        bool ShowInQuickStart(ICoreVersion core) => true;
        bool IsCompatibleWithCore(ICoreVersion core);
        bool ByDefaultHideFromQuickStart => false;

        Task<ISolutionItem?> CreateSolutionItem();
        
        async Task<IReadOnlyCollection<ISolutionItem>> CreateMultipleSolutionItems()
        {
            var item = await CreateSolutionItem();
            if (item == null)
                return Array.Empty<ISolutionItem>();
            return new[] {item};
        }
    }

    /// <summary>
    /// SolutionItemProvider for things that items that are directly related to a single specific table 
    /// </summary>
    public interface IRawDatabaseTableSolutionItemProvider : ISolutionItemProvider
    {
        string TableName { get; }
    }
    
    public interface INumberSolutionItemProvider : ISolutionItemProvider
    {
        Task<ISolutionItem?> CreateSolutionItem(long number);
        string ParameterName { get; }
    }

    [NonUniqueProvider]
    public interface IRelatedSolutionItemCreator : ISolutionItemProvider
    {
        Task<ISolutionItem?> CreateRelatedSolutionItem(RelatedSolutionItem related);
        bool CanCreatedRelatedSolutionItem(RelatedSolutionItem related);
    }
    
    // Dynamic provider of ISolutionItemProvider
    [NonUniqueProvider]
    public interface ISolutionItemProviderProvider
    {
        IEnumerable<ISolutionItemProvider> Provide();
    }

    [UniqueProvider]
    public interface ISolutionItemProvideService
    {
        IEnumerable<ISolutionItemProvider> All { get; }
        IEnumerable<ISolutionItemProvider> AllCompatible { get; }
        IEnumerable<IRelatedSolutionItemCreator> GetRelatedCreators(RelatedSolutionItem related);
    }

    public interface INamedSolutionItemProvider : ISolutionItemProvider
    {
        Task<ISolutionItem?> CreateSolutionItem(string name);
    }
}