using System;
using System.Collections.Generic;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseFieldsGroup : IDatabaseFieldsGroup
    {
        public string CategoryName { get; }
        public List<IDatabaseField> Fields { get; }
        public IObservable<bool>? GroupVisible { get; }

        public DatabaseFieldsGroup(string categoryName, List<IDatabaseField> fields, IObservable<bool>? groupVisible = null)
        {
            CategoryName = categoryName;
            Fields = fields;
            GroupVisible = groupVisible;
        }
    }
}