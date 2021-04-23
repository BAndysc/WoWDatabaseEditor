﻿using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.Models
{
    public interface IDatabaseTableData
    {
        DatabaseTableDefinitionJson TableDefinition { get; }
        IList<DatabaseEntity> Entities { get; }
    }
}