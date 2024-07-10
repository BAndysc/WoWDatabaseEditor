using System;
using System.Diagnostics;

namespace ProtoZeroGenerator;

public readonly struct ProtoType
{
    public bool IsEnum { get; }
    public bool IsMessage { get; }
    public bool IsScalar { get; }

    private readonly string name;
    private readonly BuiltinTypes builtinType;

    private ProtoType(string name, bool isEnum, bool isMessage)
    {
        Debug.Assert(isEnum || isMessage);
        this.name = name;
        IsEnum = isEnum;
        IsMessage = isMessage;
        IsScalar = false;
    }

    private ProtoType(BuiltinTypes builtinType)
    {
        this.name = null!;
        this.IsEnum = false;
        this.IsMessage = false;
        this.IsScalar = true;
        this.builtinType = builtinType;
    }

    public string Name => IsScalar ? builtinType.ToString().ToLower() : name;

    public BuiltinTypes BuiltinType => builtinType;

    public static ProtoType CreateScalar(BuiltinTypes type) => new(type);

    public static ProtoType CreateEnum(string name) => new(name, true, false);

    public static ProtoType CreateMessage(string name) => new(name, false, true);

    public string ToCSharpType(bool optional, bool asPointer)
    {
        if (IsScalar)
        {
            var cSharp = BuiltTypeToCSharp(builtinType);
            return optional ? $"{cSharp}?" : cSharp;
        }
        else if (IsEnum)
        {
            return optional ? $"{name}?" : name;
        }
        else
        {
            return optional ? (asPointer ? $"{name}*" : $"Optional<{name}>") : name;
        }
    }

    public bool IsFixedSize(out int size)
    {
        size = builtinType switch
        {
            BuiltinTypes.Double => 8,
            BuiltinTypes.Float => 4,
            BuiltinTypes.Fixed32 => 4,
            BuiltinTypes.Fixed64 => 8,
            BuiltinTypes.Sfixed32 => 4,
            BuiltinTypes.Sfixed64 => 8,
            _ => 0
        };
        return size != 0;
    }

    private static string BuiltTypeToCSharp(BuiltinTypes type)
    {
        return type switch
        {
            BuiltinTypes.Double => "double",
            BuiltinTypes.Float => "float",
            BuiltinTypes.Int32 => "int",
            BuiltinTypes.Int64 => "long",
            BuiltinTypes.UInt32 => "uint",
            BuiltinTypes.UInt64 => "ulong",
            BuiltinTypes.SInt32 => "int",
            BuiltinTypes.SInt64 => "long",
            BuiltinTypes.Fixed32 => "uint",
            BuiltinTypes.Fixed64 => "ulong",
            BuiltinTypes.Sfixed32 => "int",
            BuiltinTypes.Sfixed64 => "long",
            BuiltinTypes.Bool => "bool",
            BuiltinTypes.String => "Utf8String",
            BuiltinTypes.Bytes => "UnmanagedArray<byte>",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public bool IsPackedByDefault()
    {
        // https://protobuf.dev/programming-guides/proto3/#field-labels
        return IsScalar && builtinType != BuiltinTypes.String && builtinType != BuiltinTypes.Bytes;
    }
}

public enum BuiltinTypes
{
    Unset,
    Double,
    Float,
    Int32,
    Int64,
    UInt32,
    UInt64,
    SInt32,
    SInt64,
    Fixed32,
    Fixed64,
    Sfixed32,
    Sfixed64,
    Bool,
    String,
    Bytes,
}

public static class BuiltinTypesExtensions
{
    public static bool TryParseBuiltinType(this string type, out BuiltinTypes builtinType)
    {
        builtinType = default;
        builtinType = type switch
        {
            "double" => BuiltinTypes.Double,
            "float" => BuiltinTypes.Float,
            "int32" => BuiltinTypes.Int32,
            "int64" => BuiltinTypes.Int64,
            "uint32" => BuiltinTypes.UInt32,
            "uint64" => BuiltinTypes.UInt64,
            "sint32" => BuiltinTypes.SInt32,
            "sint64" => BuiltinTypes.SInt64,
            "fixed32" => BuiltinTypes.Fixed32,
            "fixed64" => BuiltinTypes.Fixed64,
            "sfixed32" => BuiltinTypes.Sfixed32,
            "sfixed64" => BuiltinTypes.Sfixed64,
            "bool" => BuiltinTypes.Bool,
            "string" => BuiltinTypes.String,
            "bytes" => BuiltinTypes.Bytes,
            _ => BuiltinTypes.Unset
        };
        return builtinType != BuiltinTypes.Unset;
    }
}