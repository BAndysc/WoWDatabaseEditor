using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Sessions;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Sessions.Sessions
{
    [AutoRegister]
    [SingleInstance]
    public class SessionSerializerDeserializer
    {
        private readonly ISolutionItemSerializerRegistry serializerRegistry;
        private readonly ISolutionItemDeserializerRegistry deserializerRegistry;
        private const string Begin = " -- BEGIN WDE ";
        private static readonly Regex BeginRegex = new Regex(@"^ -- BEGIN WDE (\d+);(-?\d+);(-?\d+);(.*)$");
        private const string End = " -- END WDE";

        public SessionSerializerDeserializer(ISolutionItemSerializerRegistry serializerRegistry,
            ISolutionItemDeserializerRegistry deserializerRegistry)
        {
            this.serializerRegistry = serializerRegistry;
            this.deserializerRegistry = deserializerRegistry;
        }

        public string Serialize(IEditorSession session)
        {
            StringBuilder sb = new();
            foreach (var pair in session)
            {
                var serialize = serializerRegistry.Serialize(pair.Item1, false);
                if (serialize == null)
                    throw new Exception("This solution item can't be serialized: " + pair.Item1);
                sb.AppendLine($"{Begin}{serialize.Type};{serialize.Value};{serialize.Value2 ?? 0};{serialize.StringValue}");
                sb.AppendLine(" -- " + serialize.Comment);
                sb.Append(pair.Item2);
                if (!string.IsNullOrEmpty(pair.Item2) && pair.Item2[^1] != '\n')
                    sb.AppendLine();
                sb.AppendLine(" -- END WDE");
            }

            return sb.ToString();
        }

        public IEditorSession Deserialize(IEnumerable<string> lines, IEditorSessionStub stub)
        {
            using var iterator = lines.GetEnumerator();

            StringBuilder sql = new();
            ISolutionItem? currentSolutionItem = null;
            var currentSession = new EditorSession(stub.Name, stub.FileName, stub.Created, stub.LastModified);

            while (iterator.MoveNext())
            {
                var line = iterator.Current;
                if (line.StartsWith(Begin))
                {
                    var match = BeginRegex.Match(line);
                    if (match.Success)
                    {
                        if (currentSolutionItem == null)
                        {
                            var comment = iterator.MoveNext() ? iterator.Current[4..] : "";
                            var abstractItem = new AbstractSmartScriptProjectItem()
                            {
                                Type = (byte)int.Parse(match.Groups[1].Value),
                                Value = int.Parse(match.Groups[2].Value),
                                Value2 = int.Parse(match.Groups[3].Value),
                                StringValue = match.Groups[4].Value,
                                Comment = comment
                            };
                            if (!deserializerRegistry.TryDeserialize(abstractItem, out currentSolutionItem))
                            {
                                throw new Exception($"Invalid session file (can't deserialize solution item: type: {abstractItem.Type}, value: {abstractItem.Value}/{abstractItem.Value2}/{abstractItem.StringValue})");
                            }
                        }
                        else
                            throw new Exception("Invalid session file (double BEGIN)");
                    }
                    else
                        throw new Exception("Invalid session file (can't parse BEGIN)");
                }
                else if (line.StartsWith(End))
                {
                    if (currentSolutionItem == null)
                        throw new Exception("Invalid session file (END without proper BEGIN)");
                    
                    currentSession.Insert(currentSolutionItem, sql.ToString());
                    sql.Clear();
                    currentSolutionItem = null;
                }
                else
                {
                    sql.AppendLine(line);
                }
            }

            return currentSession;
        }
    }
}