using System;
using System.Text.RegularExpressions;
using Generator.Equals;

namespace WDE.SqlWorkbench.Models.DataTypes;

[Equatable]
internal readonly partial struct SpatialDataType
{
    [DefaultEquality]
    public readonly SpatialDataTypeKind Kind;
    
    public override string ToString()
    {
        var type = Kind.ToString().ToUpper();
        return type;
    }
    
    public Type ManagedType => typeof(byte[]);
    
    public SpatialDataType(SpatialDataTypeKind kind)
    {
        Kind = kind;
    }

    public static bool TryParse(string text, out SpatialDataType dataType)
    {
        dataType = default;
        
        SpatialDataTypeKind? kind = text.ToLower() switch
        {
            "geometry" => SpatialDataTypeKind.Geometry,
            "point" => SpatialDataTypeKind.Point,
            "linestring" => SpatialDataTypeKind.LineString,
            "polygon" => SpatialDataTypeKind.Polygon,
            "multipoint" => SpatialDataTypeKind.MultiPoint,
            "multilinestring" => SpatialDataTypeKind.MultiLineString,
            "multipolygon" => SpatialDataTypeKind.MultiPolygon,
            "geometrycollection" => SpatialDataTypeKind.GeometryCollection,
            "geomcollection" => SpatialDataTypeKind.GeometryCollection,
            "geom" => SpatialDataTypeKind.GeometryCollection,
            _ => null
        };
        
        if (!kind.HasValue)
            return false;

        dataType = new SpatialDataType(kind.Value);
        
        return true;
    }
}