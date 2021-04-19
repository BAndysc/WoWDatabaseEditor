using System.Collections.Generic;

namespace WDE.DatabaseEditors.Models
{
    public interface IDatabaseFieldsGroup
    {
        string CategoryName { get; }
        List<IDatabaseField> Fields { get; }
    }
}