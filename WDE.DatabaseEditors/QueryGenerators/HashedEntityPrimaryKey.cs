using System;
using System.Collections.Generic;
using System.Linq;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.QueryGenerators;

/// <summary>
/// A helper struct used to store a hash of a primary key of a database entity
/// It is never updated, so never store it for more than a single call
/// </summary>
internal readonly struct HashedEntityPrimaryKey
{
    private readonly IList<IDatabaseField> fields;
    private readonly int hash;

    public HashedEntityPrimaryKey(DatabaseEntity entity, DatabaseTableDefinitionJson table)
    {
        fields = table.PrimaryKey!.Select(key => entity.GetCell(key)!).ToList();
        hash = 0;
        foreach (var field in fields)
            hash = HashCode.Combine(hash, field.GetHashCode());
    }

    private bool Equals(HashedEntityPrimaryKey other)
    {
        if (other.fields.Count != fields.Count)
            return false;
        for (int i = 0; i < fields.Count; ++i)
            if (!fields[i].Equals(other.fields[i]))
                return false;
        return true;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((HashedEntityPrimaryKey) obj);
    }

    public override int GetHashCode() => hash;
}