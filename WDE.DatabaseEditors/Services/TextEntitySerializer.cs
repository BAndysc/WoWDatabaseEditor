using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Factories;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

[AutoRegister]
[SingleInstance]
public class TextEntitySerializer : ITextEntitySerializer
{
    private readonly IDatabaseFieldFactory fieldFactory;

    public TextEntitySerializer(IDatabaseFieldFactory fieldFactory)
    {
        this.fieldFactory = fieldFactory;
    }
    
    public string Serialize(IEnumerable<DatabaseEntity> entities)
    {
        List<JsonFormat> data = new List<JsonFormat>();
        foreach (var entity in entities)
        {
            JsonFormat obj = new JsonFormat();
            
            foreach (var cell in entity.Cells)
                obj.values[cell.Key] = cell.Value.Object;
            
            if (entity.Conditions != null)
                obj.contitions.AddRange(entity.Conditions.Select(c => new AbstractCondition(c)));
            
            data.Add(obj);
        }

        return JsonConvert.SerializeObject(data, Formatting.Indented);
    }

    public IReadOnlyList<DatabaseEntity> Deserialize(DatabaseTableDefinitionJson definition, string json, DatabaseKey? forceKey)
    {
        List<JsonFormat>? data = JsonConvert.DeserializeObject<List<JsonFormat>>(json);

        if (data == null)
            return Array.Empty<DatabaseEntity>();

        List<DatabaseEntity> entities = new();
        foreach (var row in data)
        {
            Dictionary<ColumnFullName, IDatabaseField> fields = new Dictionary<ColumnFullName, IDatabaseField>();
            IReadOnlyList<ICondition>? conditions = (row.contitions?.Count ?? 0) == 0 ? null : row.contitions;

            foreach (var column in definition.TableColumns)
            {
                if (!column.Value.IsActualDatabaseColumn)
                    continue;
                
                if (!row.values.TryGetValue(column.Key, out var value))
                    throw new Exception("Can't find column " + column.Key + " in input string");

                fields[column.Key] = fieldFactory.CreateField(column.Value, value);
            }

            DatabaseKey key;
            
            if (definition.RecordMode == RecordMode.SingleRow)
                key = new DatabaseKey(definition.PrimaryKey.Select(keyColumn => (long)fields[keyColumn].Object!));
            else 
                key = new DatabaseKey((long)fields[definition.PrimaryKey[0]].Object!); // this is what DatabaseEntity expects to have as the key, not the best solution I must say

            if (forceKey.HasValue)
                key = forceKey.Value;

            var entity = new DatabaseEntity(false, key, fields, conditions);
            entities.Add(entity);
        }

        return entities;
    }

    private class JsonFormat
    {
        public Dictionary<ColumnFullName, object?> values = new Dictionary<ColumnFullName, object?>();
        public List<AbstractCondition> contitions = new List<AbstractCondition>();
    }
}