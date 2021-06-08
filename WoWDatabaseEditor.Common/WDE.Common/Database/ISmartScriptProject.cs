using System;

namespace WDE.Common.Database
{
    public interface ISmartScriptProject
    {
        uint Id { get; set; }
        uint? ParentId { get; set; }
        string Guid { get; set; }
        string Name { get; set; }
        DateTime UpdateDate { get; set; }
        DateTime CreateDate { get; set; }
    }
}