using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Antlr4.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ProtoZeroGenerator;

[Generator]
public class ProtobufSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    private Dictionary<string, string> packageToNamespace = new Dictionary<string, string>();

    private ProtoPrePass ExecutePrePass(string fileContent)
    {
        var inputStream = new AntlrInputStream(fileContent);
        var lexer = new Protobuf3Lexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Protobuf3Parser(commonTokenStream);
        var tree = parser.proto();

        var prepass = new ProtoPrePass();
        prepass.Visit(tree);

        if (prepass.Package != null && prepass.Namespace != null)
            packageToNamespace[prepass.Package] = prepass.Namespace;

        return prepass;
    }

    public void Execute(GeneratorExecutionContext context)
    {
        HashSet<string> imported = new();
        Queue<string> toImport = new();

        foreach (var additionalFile2 in context.AdditionalFiles)
        {
            if (additionalFile2.Path.EndsWith(".proto"))
            {
                string? protoFileContent = additionalFile2.GetText(context.CancellationToken)?.ToString();

                if (!string.IsNullOrEmpty(protoFileContent))
                {
                    var prepass = ExecutePrePass(protoFileContent);
                    foreach (var import in prepass.Imports)
                    {
                        toImport.Enqueue(import);
                    }
                }
            }
        }

        while (toImport.Count > 0)
        {
            var importFile = toImport.Dequeue();
            if (!imported.Add(importFile))
                continue;

            string protoFileContent;
            if (importFile == "google/protobuf/timestamp.proto")
                protoFileContent = KnownProtos.Timestamp;
            else if (importFile == "google/protobuf/wrappers.proto")
                protoFileContent = KnownProtos.Wrappers;
            else
                protoFileContent = File.ReadAllText(importFile);

            var prepass = ExecutePrePass(protoFileContent);
            foreach (var import in prepass.Imports)
            {
                toImport.Enqueue(import);
            }
        }

        foreach (var additionalFile in context.AdditionalFiles)
        {
            if (additionalFile.Path.EndsWith(".proto"))
            {
                string? protoFileContent = additionalFile.GetText(context.CancellationToken)?.ToString();

                if (!string.IsNullOrEmpty(protoFileContent))
                {
                    string code = GenerateProtobufClasses(protoFileContent);

                    context.AddSource($"{Path.GetFileNameWithoutExtension(additionalFile.Path)}.g.cs", SourceText.From(code, Encoding.UTF8));
                }
            }
        }

        foreach (var importedFile in imported)
        {
            string protoFileContent;
            if (importedFile == "google/protobuf/timestamp.proto")
                protoFileContent = KnownProtos.Timestamp;
            else if (importedFile == "google/protobuf/wrappers.proto")
                protoFileContent = KnownProtos.Wrappers;
            else
                protoFileContent = File.ReadAllText(importedFile);

            string code = GenerateProtobufClasses(protoFileContent);
            context.AddSource($"{Path.GetFileNameWithoutExtension(importedFile)}.g.cs", SourceText.From(code, Encoding.UTF8));
        }
    }

    public struct Unit
    {
    }

    public class ProtoPrePass : Protobuf3BaseVisitor<Unit>
    {
        public List<string> Enums { get; } = new List<string>();

        public List<string> Imports { get; } = new List<string>();

        public string Package { get; private set; }

        public string Namespace { get; private set; }

        public override Unit VisitImportStatement(Protobuf3Parser.ImportStatementContext context)
        {
            Imports.Add(context.strLit().GetText().Trim('"', '\''));
            return base.VisitImportStatement(context);
        }

        public override Unit VisitPackageStatement(Protobuf3Parser.PackageStatementContext context)
        {
            Package = context.fullIdent().GetText();
            return base.VisitPackageStatement(context);
        }

        public override Unit VisitOptionStatement(Protobuf3Parser.OptionStatementContext context)
        {
            if (context.optionName().GetText() == "csharp_namespace")
            {
                Namespace = context.constant().GetText().Trim('"', '\'');
            }

            return base.VisitOptionStatement(context);
        }

        public override Unit VisitEnumDef(Protobuf3Parser.EnumDefContext context)
        {
            Enums.Add(context.enumName().GetText());
            return base.VisitEnumDef(context);
        }
    }

    public class ProtobufVisitor : Protobuf3BaseVisitor<object>
    {
        private readonly ProtobufSourceGenerator generator;
        private readonly List<string> enums;

        public ProtobufVisitor(ProtobufSourceGenerator generator, List<string> enums)
        {
            this.generator = generator;
            this.enums = enums;
        }

        public List<MessageDefinition> Messages { get; } = new List<MessageDefinition>();
        public List<EnumDefinition> Enums { get; } = new List<EnumDefinition>();

        public override object VisitExtendDef(Protobuf3Parser.ExtendDefContext context)
        {
            throw new NotImplementedException("This feature is not yet implemented in the generator");
        }

        public override object VisitServiceDef(Protobuf3Parser.ServiceDefContext context)
        {
            throw new NotImplementedException("This feature is not yet implemented in the generator");
        }

        private ProtoType Parse(string type)
        {
            if (type.TryParseBuiltinType(out var builtinType))
                return ProtoType.CreateScalar(builtinType);
            if (enums.Contains(type))
                return ProtoType.CreateEnum(type);

            foreach (var pair in generator.packageToNamespace)
            {
                var package = pair.Key;
                var @namespace = pair.Value;
                if (type.StartsWith(package + "."))
                    type = @namespace + type.Substring(package.Length);
            }
            return ProtoType.CreateMessage(type);
        }

        private string GetUniqueFieldName(string messageName, string fieldName)
        {
            if (messageName == fieldName)
                return fieldName + "_";
            return fieldName;
        }

        public override object VisitMessageDef(Protobuf3Parser.MessageDefContext context)
        {
            var message = new MessageDefinition
            {
                Name = context.messageName().GetText().ToTitleCase()
            };

            foreach (var element in context.messageBody().messageElement())
            {
                if (element.field() is { } field)
                {
                    var fieldTypeStr = field.type_().GetText();
                    var fieldType = Parse(fieldTypeStr);
                    var packedOption = field.fieldOptions()?.fieldOption()?.FirstOrDefault(o =>
                        string.Equals(o.optionName().GetText(), "packed", StringComparison.OrdinalIgnoreCase));
                    var asWellKnownWrapperOption = field.fieldOptions()?.fieldOption().FirstOrDefault(o =>
                        string.Equals(o.optionName().GetText(), "AsWellKnownWrapper", StringComparison.OrdinalIgnoreCase));
                    message.Fields.Add(new FieldDefinition
                    {
                        Name = GetUniqueFieldName(message.Name, field.fieldName().GetText().ToTitleCase()),
                        Number = ParseNumber(field.fieldNumber().GetText()),
                        Type = fieldType,
                        IsRepeated = field.fieldLabel()?.REPEATED() != null,
                        IsOptional = field.fieldLabel()?.OPTIONAL() != null,
                        IsPacked = packedOption != null
                            ? string.Equals(packedOption.constant().GetText(), "true",
                                StringComparison.OrdinalIgnoreCase)
                            : fieldType.IsPackedByDefault(),
                        AsWellKnownWrapper = asWellKnownWrapperOption != null &&
                                            string.Equals(asWellKnownWrapperOption.constant().GetText(), "true",
                                                StringComparison.OrdinalIgnoreCase),
                    });
                }
                else if (element.oneof() is { } oneof)
                {
                    var oneofDefinition = new OneofDefinition
                    {
                        Name = oneof.oneofName().GetText().ToTitleCase()
                    };

                    var unionOption = oneof.optionStatement()?.FirstOrDefault(o => o.optionName().GetText() == "union");

                    if (unionOption != null)
                    {
                        oneofDefinition.Inline = string.Equals(unionOption.constant().GetText(), "true", StringComparison.OrdinalIgnoreCase);
                    }

                    foreach (var oneofField in oneof.oneofField())
                    {
                        oneofDefinition.Fields.Add(new FieldDefinition
                        {
                            Name = oneofField.fieldName().GetText().ToTitleCase(),
                            Number = ParseNumber(oneofField.fieldNumber().GetText()),
                            Type = Parse(oneofField.type_().GetText())
                        });
                    }

                    message.Oneofs.Add(oneofDefinition);
                }
                else if (element.mapField() is { } map)
                {
                    var mapDefinition = new MapDefinition()
                    {
                        Name = map.mapName().GetText().ToTitleCase(),
                        Number = ParseNumber(map.fieldNumber().GetText()),
                        KeyType = Parse(map.keyType().GetText()),
                        ValueType = Parse(map.type_().GetText())
                    };

                    message.Maps.Add(mapDefinition);
                }
                else if (element.optionStatement() is { } option)
                {
                    if (option.optionName().GetText() == "equality")
                        message.GenerateEquality =
                            string.Equals(option.constant().GetText(), "true", StringComparison.OrdinalIgnoreCase);
                }
                else if (element.emptyStatement_() != null)
                    continue;
                else
                    throw new NotImplementedException($"{element.GetText()} is not supported in the generator yet");
            }

            Messages.Add(message);
            return base.VisitMessageDef(context);
        }

        public override object VisitEnumDef(Protobuf3Parser.EnumDefContext context)
        {
            var enumDef = new EnumDefinition
            {
                Name = context.enumName().GetText().ToTitleCase()
            };

            foreach (var element in context.enumBody().enumElement())
            {
                var enumField = element.enumField();
                if (enumField != null)
                {
                    enumDef.Values.Add(new EnumValue
                    {
                        Name = enumField.ident().GetText(),
                        Number = ParseNumber(enumField.intLit().GetText())
                    });
                }
            }

            Enums.Add(enumDef);
            return base.VisitEnumDef(context);
        }

        private int ParseNumber(string number)
        {
            if (number.StartsWith("0x") || number.StartsWith("0X"))
            {
                return int.Parse(number.Substring(2), System.Globalization.NumberStyles.HexNumber);
            }

            return int.Parse(number);
        }
    }

    private string GenerateProtobufClasses(string protoFileContent)
    {
        var inputStream = new AntlrInputStream(protoFileContent);
        var lexer = new Protobuf3Lexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Protobuf3Parser(commonTokenStream);
        var tree = parser.proto();

        var prepass = new ProtoPrePass();
        prepass.Visit(tree);

        var visitor = new ProtobufVisitor(this, prepass.Enums);
        visitor.Visit(tree);

        var codeGenerator = new CodeGenerator();
        codeGenerator.AppendLine("#nullable enable");
        codeGenerator.AppendLine("using System;");
        codeGenerator.AppendLine("using ProtoZeroSharp;");
        codeGenerator.AppendLine("using System.Runtime.CompilerServices;");
        codeGenerator.AppendLine("using System.Runtime.InteropServices;");

        if (!string.IsNullOrEmpty(prepass.Namespace))
            codeGenerator.OpenBlock($"namespace {prepass.Namespace}");

        foreach (var enumDef in visitor.Enums)
        {
            codeGenerator.OpenBlock($"public enum {enumDef.Name}");
            foreach (var value in enumDef.Values)
                codeGenerator.AppendLine($"{value.Name} = {value.Number},");
            codeGenerator.CloseBlock();
        }

        foreach (var message in visitor.Messages)
        {
            var inherits = "";
            if (message.GenerateEquality)
                inherits = " : IEquatable<" + message.Name + ">";
            codeGenerator.OpenBlock($"public unsafe partial struct {message.Name}{inherits}");

            if (prepass.Namespace == "Google.Protobuf.WellKnownTypes" && message.Name == "Timestamp")
            {
                codeGenerator.AppendLine("private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);");
                codeGenerator.OpenBlock("private static bool IsNormalized(long seconds, int nanoseconds)");
                codeGenerator.AppendLine("return nanoseconds >= 0 && nanoseconds <= 999999999 && seconds >= -62135596800L && seconds <= 253402300799L;");
                codeGenerator.CloseBlock();
                codeGenerator.OpenBlock("public DateTime ToDateTime()");
                codeGenerator.AppendLine("if (!Timestamp.IsNormalized(this.Seconds, this.Nanos))");
                codeGenerator.AppendLine("    throw new InvalidOperationException(\"Timestamp contains invalid values: Seconds={Seconds}; Nanos={Nanos}\");");
                codeGenerator.AppendLine("return Timestamp.UnixEpoch.AddSeconds((double) this.Seconds).AddTicks((long) (this.Nanos / 100));");
                codeGenerator.CloseBlock();
            }

            foreach (var field in message.Fields)
            {
                var fieldType = field.Type.ToCSharpType(field.IsOptional, field.IsOptionalPointer);
                if (field.IsRepeated)
                    fieldType = $"UnmanagedArray<{fieldType}>";
                codeGenerator.AppendLine($"public {fieldType} {field.Name};");
            }

            foreach (var map in message.Maps)
            {
                var keyType = map.KeyType.ToCSharpType(false, false);
                var valueType = map.ValueType.ToCSharpType(false, false);
                codeGenerator.AppendLine($"public UnmanagedMap<{keyType}, {valueType}> {map.Name};");
            }

            codeGenerator.OpenBlock("public static class FieldIds");
            foreach (var field in message.Fields)
            {
                codeGenerator.AppendLine($"public const int {field.Name} = {field.Number};");
            }

            foreach (var oneof in message.Oneofs)
            {
                foreach (var field in oneof.Fields)
                {
                    codeGenerator.AppendLine($"public const int {field.Name} = {field.Number};");
                }
            }

            foreach (var map in message.Maps)
            {
                codeGenerator.AppendLine($"public const int {map.Name} = {map.Number};");
            }

            codeGenerator.CloseBlock();

            foreach (var oneof in message.Oneofs)
            {
                var oneofName = oneof.Name;
                codeGenerator.AppendLine($"public {oneofName}OneofCase {oneofName}Case;");

                if (oneof.Inline)
                {
                    codeGenerator.AppendLine($"private {oneofName}Union {oneofName}Data;");
                    codeGenerator.AppendLine("[StructLayout(LayoutKind.Explicit)]");
                    codeGenerator.OpenBlock($"private struct {oneofName}Union");
                    foreach (var type in oneof.Fields)
                    {
                        codeGenerator.AppendLine($"[FieldOffset(0)] public {type.Type.ToCSharpType(false, false)} {type.Name};");
                    }
                    codeGenerator.CloseBlock();
                }
                else
                {
                    codeGenerator.AppendLine($"private byte* {oneofName}Data;");
                }

                codeGenerator.OpenBlock($"public enum {oneofName}OneofCase");
                codeGenerator.AppendLine("None,");
                foreach (var field in oneof.Fields)
                {
                    codeGenerator.AppendLine($"{field.Name},");
                }

                codeGenerator.CloseBlock();

                codeGenerator.OpenBlock($"public ref T {oneofName}<T>() where T : unmanaged");
                codeGenerator.AppendLine("#if DEBUG || !DEBUG");
                codeGenerator.AppendLine($"if ({oneofName}Case == {oneofName}OneofCase.None)");
                codeGenerator.AppendLine("throw new NullReferenceException();");
                foreach (var field in oneof.Fields)
                {
                    codeGenerator.AppendLine(
                        $"if ({oneofName}Case == {oneofName}OneofCase.{field.Name} && typeof(T) != typeof({field.Type.ToCSharpType(false, false)}))");
                    codeGenerator.AppendLine("throw new InvalidCastException();");
                }

                if (!oneof.Inline)
                {
                    codeGenerator.AppendLine($"if ({oneofName}Data == null)");
                    codeGenerator.AppendLine($"throw new NullReferenceException();");
                }
                codeGenerator.AppendLine("#endif");
                if (oneof.Inline)
                {
                    codeGenerator.AppendLine("#pragma warning disable CS9084");
                    codeGenerator.AppendLine($"return ref Unsafe.As<{oneofName}Union, T>(ref {oneofName}Data);");
                    codeGenerator.AppendLine("#pragma warning restore CS9084");
                }
                else
                {
                    codeGenerator.AppendLine($"return ref Unsafe.AsRef<T>({oneofName}Data);");
                }
                codeGenerator.CloseBlock();

                if (!oneof.Inline)
                {
                    codeGenerator.OpenBlock($"public ref T Alloc{oneofName}<T>(ref ArenaAllocator memory) where T : unmanaged");
                    codeGenerator.AppendLine($"var mem = memory.TakeContiguousSpan(sizeof(T));");
                    codeGenerator.AppendLine($"{oneofName}Data = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(mem));");
                    codeGenerator.AppendLine($"return ref Unsafe.AsRef<T>({oneofName}Data);");
                    codeGenerator.CloseBlock();
                }

                foreach (var field in oneof.Fields)
                    codeGenerator.AppendLine($"public ref {field.Type.ToCSharpType(false, false)} {field.Name} => ref {oneofName}<{field.Type.ToCSharpType(false, false)}>();");
            }

            codeGenerator.OpenBlock("internal unsafe void Read(ref ProtoReader reader, ref ArenaAllocator memory)");

            foreach (var field in message.Fields.Where(f => f.IsRepeated))
            {
                codeGenerator.AppendLine($"int {field.Name}_count = 0;");
            }

            foreach (var map in message.Maps)
            {
                codeGenerator.AppendLine($"int {map.Name}_count = 0;");
            }

            if (message.Fields.Any(f => f.IsRepeated) || message.Maps.Count > 0)
            {
                codeGenerator.AppendLine("ProtoReader readerCopy = reader;");
                codeGenerator.OpenBlock("while (readerCopy.Next())");
                codeGenerator.OpenBlock("switch (readerCopy.Tag)");
                foreach (var field in message.Fields.Where(f => f.IsRepeated))
                {
                    codeGenerator.OpenBlock($"case FieldIds.{field.Name}:");
                    if (field.IsPacked)
                    {
                        codeGenerator.AppendLine("var subReader = readerCopy.ReadMessage();");
                        if (field.Type.IsFixedSize(out var fixedSize))
                        {
                            codeGenerator.AppendLine($"{field.Name}_count += subReader.Remaining / {fixedSize};");
                        }
                        else
                        {
                            codeGenerator.OpenBlock("while (subReader.Remaining > 0)");
                            codeGenerator.AppendLine("subReader.ReadVarInt();");
                            codeGenerator.AppendLine($"{field.Name}_count++;");
                            codeGenerator.CloseBlock();
                        }
                    }
                    else
                    {
                        codeGenerator.AppendLine($"{field.Name}_count++;");
                        codeGenerator.AppendLine("readerCopy.Skip();");
                    }

                    codeGenerator.AppendLine("break;").CloseBlock();
                }

                foreach (var map in message.Maps)
                {
                    codeGenerator.OpenBlock($"case FieldIds.{map.Name}:");
                    codeGenerator.AppendLine($"{map.Name}_count++;");
                    codeGenerator.AppendLine("readerCopy.Skip();");
                    codeGenerator.AppendLine("break;").CloseBlock();
                }

                codeGenerator.AppendLine("default:");
                codeGenerator.AppendLine("readerCopy.Skip();");
                codeGenerator.AppendLine("break;");
                codeGenerator.CloseBlock();
                codeGenerator.CloseBlock();
            }

            foreach (var field in message.Fields.Where(f => f.IsRepeated))
            {
                codeGenerator.AppendLine(
                    $"{field.Name} = UnmanagedArray<{field.Type.ToCSharpType(false, false)}>.AllocArray({field.Name}_count, ref memory);");
                codeGenerator.AppendLine($"{field.Name}_count = 0;");
            }

            foreach (var map in message.Maps)
            {
                var keyType = map.KeyType.ToCSharpType(false, false);
                var valueType = map.ValueType.ToCSharpType(false, false);
                codeGenerator.AppendLine(
                    $"{map.Name} = UnmanagedMap<{keyType}, {valueType}>.AllocMap({map.Name}_count, ref memory);");
                codeGenerator.AppendLine($"{map.Name}_count = 0;");
                codeGenerator.AppendLine(
                    $"{map.Name}.GetUnderlyingArrays(out var {map.Name}_keys, out var {map.Name}_values);");
            }

            codeGenerator.OpenBlock("while (reader.Next())");
            codeGenerator.OpenBlock("switch (reader.Tag)");

            string ReadType(ProtoType type, string reader)
            {
                if (type.IsEnum)
                    return $"({type.Name}){reader}.ReadVarInt()";

                return type.BuiltinType switch
                {
                    BuiltinTypes.Double => $"{reader}.ReadDouble()",
                    BuiltinTypes.Float => $"{reader}.ReadFloat()",
                    BuiltinTypes.Int32 => $"(int){reader}.ReadVarInt()",
                    BuiltinTypes.Int64 => $"(long){reader}.ReadVarInt()",
                    BuiltinTypes.UInt32 => $"(uint){reader}.ReadVarInt()",
                    BuiltinTypes.UInt64 => $"(ulong){reader}.ReadVarInt()",
                    BuiltinTypes.SInt32 => $"(int){reader}.ReadZigZag()",
                    BuiltinTypes.SInt64 => $"(long){reader}.ReadZigZag()",
                    BuiltinTypes.Fixed32 => $"(uint){reader}.ReadFixed32()",
                    BuiltinTypes.Fixed64 => $"(ulong){reader}.ReadFixed64()",
                    BuiltinTypes.Sfixed32 => $"(int){reader}.ReadFixed32()",
                    BuiltinTypes.Sfixed64 => $"(long){reader}.ReadFixed64()",
                    BuiltinTypes.Bool => $"{reader}.ReadBool()",
                    BuiltinTypes.String => $"{reader}.ReadUtf8String(ref memory)",
                    BuiltinTypes.Bytes => $"{reader}.ReadBytesArray(ref memory)",
                    _ => throw new NotImplementedException($"Type {type} is not supported")
                };
            }

            foreach (var field in message.Fields)
            {
                codeGenerator.OpenBlock($"case FieldIds.{field.Name}:");
                if (field.IsRepeated)
                {
                    if (field.AsWellKnownWrapper)
                    {
                        throw new NotImplementedException("I didn't implement repeated well known wrappers yet");
                    }
                    if (field.IsPacked)
                    {
                        if (!field.Type.IsScalar)
                            throw new InvalidOperationException("I thought only scalar types could be packed?");
                        codeGenerator.AppendLine("var subReader = reader.ReadMessage();");
                        codeGenerator.OpenBlock("while (subReader.Remaining > 0)");
                        codeGenerator.AppendLine(
                            $"{field.Name}[{field.Name}_count++] = {ReadType(field.Type, "subReader")};");
                        codeGenerator.CloseBlock();
                    }
                    else if (field.Type.IsMessage)
                    {
                        codeGenerator.AppendLine("var subReader = reader.ReadMessage();");
                        codeGenerator.AppendLine($"{field.Name}[{field.Name}_count] = default;");
                        codeGenerator.AppendLine(
                            $"{field.Name}[{field.Name}_count++].Read(ref subReader, ref memory);");
                    }
                    else
                    {
                        codeGenerator.AppendLine(
                            $"{field.Name}[{field.Name}_count++] = {ReadType(field.Type, "reader")};");
                    }
                }
                else
                {
                    if (field.Type.IsMessage)
                    {
                        codeGenerator.AppendLine("var subReader = reader.ReadMessage();");
                        if (field.IsOptional)
                        {
                            if (field.IsOptionalPointer)
                            {
                                var fieldType = field.Type.ToCSharpType(false, false);
                                codeGenerator.AppendLine(
                                    $"var {field.Name}_span = memory.TakeContiguousSpan(sizeof({fieldType}));");
                                codeGenerator.AppendLine($"{field.Name} = ({fieldType}*)Unsafe.AsPointer(ref MemoryMarshal.GetReference({field.Name}_span));");
                                codeGenerator.AppendLine($"*{field.Name} = default;");
                                codeGenerator.AppendLine($"{field.Name}->Read(ref subReader, ref memory);");
                            }
                            else
                            {
                                codeGenerator.AppendLine($"{field.Name}.Value = default;");
                                codeGenerator.AppendLine($"{field.Name}.Value.Read(ref subReader, ref memory);");
                                codeGenerator.AppendLine($"{field.Name}.HasValue = true;");
                            }
                        }
                        else
                        {
                            codeGenerator.AppendLine($"{field.Name} = default;");
                            codeGenerator.AppendLine($"{field.Name}.Read(ref subReader, ref memory);");
                        }
                    }
                    else
                    {
                        if (field.AsWellKnownWrapper)
                        {
                            codeGenerator.AppendLine("var subReader = reader.ReadMessage();");
                            codeGenerator.AppendLine("if (!subReader.Next()) break; ");
                            codeGenerator.AppendLine("if (subReader.Tag != 1) { throw new InvalidOperationException(\"Can't read well known wrapper, because of an invalid tag\"); }");
                            codeGenerator.AppendLine($"{field.Name} = {ReadType(field.Type, "subReader")};");
                        }
                        else
                        {
                            codeGenerator.AppendLine($"{field.Name} = {ReadType(field.Type, "reader")};");
                        }
                    }
                }

                codeGenerator.AppendLine("break;").CloseBlock();
            }

            foreach (var map in message.Maps)
            {
                codeGenerator.OpenBlock($"case FieldIds.{map.Name}:");
                codeGenerator.AppendLine($"var subReader = reader.ReadMessage();");
                codeGenerator.OpenBlock($"while (subReader.Next())");
                codeGenerator.OpenBlock("switch (subReader.Tag)");
                codeGenerator.AppendLine("case 1:");
                codeGenerator.AppendLine($"{map.Name}_keys[{map.Name}_count] = {ReadType(map.KeyType, "subReader")};");
                codeGenerator.AppendLine("break;");
                codeGenerator.AppendLine("case 2:");
                if (map.ValueType.IsMessage)
                {
                    codeGenerator.AppendLine("var subSubReader = subReader.ReadMessage();");
                    codeGenerator.AppendLine($"{map.Name}_values[{map.Name}_count] = default;");
                    codeGenerator.AppendLine(
                        $"{map.Name}_values[{map.Name}_count].Read(ref subSubReader, ref memory);");
                }
                else
                {
                    codeGenerator.AppendLine(
                        $"{map.Name}_values[{map.Name}_count] = {ReadType(map.ValueType, "subReader")};");
                }

                codeGenerator.AppendLine("break;");

                codeGenerator.CloseBlock();
                codeGenerator.CloseBlock();
                codeGenerator.AppendLine($"{map.Name}_count++;");
                codeGenerator.AppendLine("break;");
                codeGenerator.CloseBlock();
            }

            // Handle oneof fields
            foreach (var oneof in message.Oneofs)
            {
                foreach (var field in oneof.Fields)
                {
                    codeGenerator.OpenBlock($"case FieldIds.{field.Name}:");
                    var oneofName = oneof.Name.ToTitleCase();
                    codeGenerator.AppendLine($"{oneofName}Case = {oneofName}OneofCase.{field.Name};");

                    if (oneof.Inline)
                    {
                        if (field.Type.IsMessage)
                        {
                            codeGenerator.AppendLine($"{oneof.Name}Data.{field.Name} = default;");
                            codeGenerator.AppendLine($"var subReader = reader.ReadMessage();");
                            codeGenerator.AppendLine($"{oneof.Name}Data.{field.Name}.Read(ref subReader, ref memory);");
                        }
                        else
                        {
                            codeGenerator.AppendLine($"{oneof.Name}Data.{field.Name} = {ReadType(field.Type, "reader")};");
                        }
                    }
                    else
                    {
                        codeGenerator.AppendLine($"var mem = memory.TakeContiguousSpan(sizeof({field.Type.ToCSharpType(false, false)}));");
                        codeGenerator.AppendLine($"{oneof.Name}Data = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(mem));");
                        codeGenerator.AppendLine($"{field.Type.ToCSharpType(false, false)}* ptr = ({field.Type.ToCSharpType(false, false)}*){oneof.Name}Data;");
                        if (field.Type.IsMessage)
                        {
                            codeGenerator.AppendLine("var subReader = reader.ReadMessage();");
                            codeGenerator.AppendLine("*ptr = default;");
                            codeGenerator.AppendLine("ptr->Read(ref subReader, ref memory);");
                        }
                        else
                        {
                            codeGenerator.AppendLine($"*ptr = {ReadType(field.Type, "reader")};");
                        }
                    }

                    codeGenerator.AppendLine("break;").CloseBlock();
                }
            }

            codeGenerator.AppendLine("default:");
            codeGenerator.AppendLine("reader.Skip();");
            codeGenerator.AppendLine("break;");
            codeGenerator.CloseBlock();
            codeGenerator.CloseBlock();

            codeGenerator.CloseBlock();

            if (!message.GenerateEquality)
                codeGenerator.AppendLine("// no equality, because it is opt out be default, set option equality=true");
            else if (message.Maps.Count > 0)
                codeGenerator.AppendLine("// no equality, because of the map field");
            else if (message.Oneofs.Any(x => !x.Inline))
                codeGenerator.AppendLine("// no equality, because of not inline one of field");
            else if (message.Fields.Any(x => x.IsRepeated))
                codeGenerator.AppendLine("// no equality, because of repeated field");
            else
            {
                List<string> toCombine = new();
                foreach (var field in message.Fields)
                {
                    toCombine.Add($"{field.Name}");
                }
                foreach (var oneOf in message.Oneofs)
                {
                    toCombine.Add($"{oneOf.Name}Case");
                    foreach (var field in oneOf.Fields)
                    {
                        toCombine.Add($"{oneOf.Name}Case == {oneOf.Name}OneofCase.{field.Name} ? {field.Name}.GetHashCode() : 0");
                    }
                }

                if (toCombine.Count > 8)
                {
                    codeGenerator.AppendLine("// no equality, because more than 8 fields and HashCode.Combine supports only 8");
                }
                else
                {
                    codeGenerator.AppendLine($"public static bool operator ==({message.Name} left, {message.Name} right) => left.Equals(right);");
                    codeGenerator.AppendLine($"public static bool operator !=({message.Name} left, {message.Name} right) => !left.Equals(right);");

                    codeGenerator.OpenBlock($"public bool Equals({message.Name} other)");
                    List<string> equalities = new();
                    foreach (var field in message.Fields)
                    {
                        equalities.Add($"{field.Name} == other.{field.Name}");
                    }
                    foreach (var oneOf in message.Oneofs)
                    {
                        List<string> inner = new();
                        foreach (var field in oneOf.Fields)
                        {
                            inner.Add($"{oneOf.Name}Case == {oneOf.Name}OneofCase.{field.Name} && {field.Name} == other.{field.Name}");
                        }
                        equalities.Add($"{oneOf.Name}Case == other.{oneOf.Name}Case && ({string.Join(" || ", inner)})");
                    }

                    codeGenerator.AppendLine($"return " + string.Join(" &&\n            ", equalities) + ";");
                    codeGenerator.CloseBlock();


                    codeGenerator.OpenBlock($"public override bool Equals(object? obj)");
                    codeGenerator.AppendLine($"return obj is {message.Name} other && Equals(other);");
                    codeGenerator.CloseBlock();


                    codeGenerator.OpenBlock($"public override int GetHashCode()");
                    codeGenerator.AppendLine($"return HashCode.Combine({string.Join(", \n            ", toCombine)});");
                    codeGenerator.CloseBlock();
                }
            }

            codeGenerator.CloseBlock();
        }

        if (!string.IsNullOrEmpty(prepass.Namespace))
            codeGenerator.CloseBlock();

        codeGenerator.AppendLine("#nullable restore");
        return codeGenerator.ToString();
    }

    public class OneofDefinition
    {
        public string Name { get; set; }
        public List<FieldDefinition> Fields { get; } = new List<FieldDefinition>();
        public bool Inline { get; set; } = false;
    }

    public class MapDefinition
    {
        public string Name { get; set; }
        public int Number { get; set; }
        public ProtoType KeyType { get; set; }
        public ProtoType ValueType { get; set; }
    }

    public class MessageDefinition
    {
        public string Name { get; set; }
        public bool GenerateEquality { get; set; }
        public List<FieldDefinition> Fields { get; } = new List<FieldDefinition>();
        public List<OneofDefinition> Oneofs { get; } = new List<OneofDefinition>();
        public List<MapDefinition> Maps { get; } = new List<MapDefinition>();
    }

    public class FieldDefinition
    {
        public string Name { get; set; }
        public int Number { get; set; }
        public ProtoType Type { get; set; }
        public bool IsRepeated { get; set; }
        public bool IsOptional { get; set; }
        public bool IsOptionalPointer { get; set; } = true;
        public bool IsPacked { get; set; }
        public bool AsWellKnownWrapper { get; set; }
    }

    public class EnumDefinition
    {
        public string Name { get; set; }
        public List<EnumValue> Values { get; } = new List<EnumValue>();
    }

    public class EnumValue
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }
}