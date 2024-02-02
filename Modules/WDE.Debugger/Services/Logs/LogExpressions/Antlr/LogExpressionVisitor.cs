using System;
using Newtonsoft.Json.Linq;

namespace WDE.Debugger.Services.Logs.LogExpressions.Antlr;

internal class LogExpressionVisitor : LogExpressionBaseVisitor<JToken>
{
    private JObject? rootObject;

    public void SetRoot(JObject? rootObject)
    {
        this.rootObject = rootObject;
    }

    public override JToken VisitEId(LogExpressionParser.EIdContext context)
    {
        var id = context.ID().GetText();
        if (rootObject == null)
            return JValue.CreateNull();

        if (id == null)
            return JValue.CreateNull();

        if (string.Equals(id, "this", StringComparison.OrdinalIgnoreCase) ||
            id == "$")
            return rootObject;

        if (rootObject.TryGetValue(id, out var value))
            return value;

        return JValue.CreateNull();
    }

    public override JToken VisitEMulOp(LogExpressionParser.EMulOpContext context)
    {
        var va = Visit(context.expr()[0]);
        var vb = Visit(context.expr()[1]);

        if (va is JValue a && a.Type == JTokenType.Integer && vb is JValue b && b.Type == JTokenType.Integer)
            return a.Value<int>() * b.Value<int>();

        throw new LogParseException($"Trying to multiply non-integer values: {va.Type} * {vb.Type}");
    }

    public override JToken VisitEDivOp(LogExpressionParser.EDivOpContext context)
    {
        var va = Visit(context.expr()[0]);
        var vb = Visit(context.expr()[1]);

        if (va is JValue a && a.Type == JTokenType.Integer && vb is JValue b && b.Type == JTokenType.Integer)
            return a.Value<int>() / b.Value<int>();

        throw new LogParseException($"Trying to divide non-integer values: {va.Type} / {vb.Type}");
    }

    public override JToken VisitEObjectField(LogExpressionParser.EObjectFieldContext context)
    {
        var obj = Visit(context.expr());
        var field = context.ID().GetText();
        if (obj is JObject jobj)
        {
            if (jobj.TryGetValue(field, out var value))
                return value;
            throw new LogParseException($"Trying to access a non existing field {field} in object {obj}");
        }
        else if (obj.Type == JTokenType.Null)
            return obj; // propagate null
        else if (obj.Type == JTokenType.Array && (
                     field.Equals("count", StringComparison.OrdinalIgnoreCase) ||
                     field.Equals("size", StringComparison.OrdinalIgnoreCase) ||
                     field.Equals("length", StringComparison.OrdinalIgnoreCase)))
            return ((JArray)obj).Count;

        throw new LogParseException($"Trying to access a field of a non-object ({obj.Type}): {obj}");
    }

    public override JToken VisitEParen(LogExpressionParser.EParenContext context)
    {
        return Visit(context.expr());
    }

    public override JToken VisitEArrayAccess(LogExpressionParser.EArrayAccessContext context)
    {
        var obj = Visit(context.expr()[0]);
        var index = Visit(context.expr()[1]);

        if (obj == null)
            return JValue.CreateNull(); // propagate null

        if (index is not JValue indexValue || indexValue.Type != JTokenType.Integer)
            throw new LogParseException("Array index is not a number: " + index.Type);

        var asInt = indexValue.Value<int>();

        if (obj is JValue sValue && sValue.Type == JTokenType.String)
        {
            var s = (string)sValue.Value!;
            if (asInt >= 0 && asInt < s.Length)
                return s[asInt];
            return JValue.CreateNull();
        }

        if (obj is JArray array)
        {
            if (asInt >= 0 && asInt < array.Count)
                return array[asInt];
            return JValue.CreateNull();
        }

        throw new LogParseException($"Trying to access an array element of a non-array ({obj.Type}): {obj}");
    }

    public override JToken VisitEInt(LogExpressionParser.EIntContext context)
    {
        if (!int.TryParse(context.INT().GetText(), out var asInt))
            throw new LogParseException("Integer out of range: " + context.INT().GetText());
        return asInt;
    }

    public override JToken VisitEPlusOp(LogExpressionParser.EPlusOpContext context)
    {
        var va = Visit(context.expr()[0]);
        var vb = Visit(context.expr()[1]);

        if (va is JValue a && a.Type == JTokenType.Integer && vb is JValue b && b.Type == JTokenType.Integer)
            return a.Value<int>() + b.Value<int>();

        return $"{va}{vb}";
    }

    public override JToken VisitEConcat(LogExpressionParser.EConcatContext context)
    {
        var va = Visit(context.expr()[0]);
        var vb = Visit(context.expr()[1]);
        return $"{va}{vb}";
    }

    public override JToken VisitEStr(LogExpressionParser.EStrContext context)
    {
        return context.STRING().GetText()[1..^1];
    }

    private bool CompareIntegers(LogExpressionParser.ExprContext[] expr, Func<int, int, bool> compare)
    {
        var va = Visit(expr[0]);
        var vb = Visit(expr[1]);
        if (va is JValue a && a.Type == JTokenType.Integer && vb is JValue b && b.Type == JTokenType.Integer)
            return compare(a.Value<int>(), b.Value<int>());
        throw new LogParseException($"Can't compare non-integer values (got {va.Type} and {vb.Type})");
    }

    private bool CompareBool(LogExpressionParser.ExprContext[] expr, Func<bool, bool, bool> compare)
    {
        var va = Visit(expr[0]);
        var vb = Visit(expr[1]);
        if (va is JValue a && a.Type == JTokenType.Boolean && vb is JValue b && b.Type == JTokenType.Boolean)
            return compare(a.Value<bool>(), b.Value<bool>());
        throw new LogParseException($"Can't compare non-bool values (got {va.Type} and {vb.Type})");
    }

    private bool Compare(LogExpressionParser.ExprContext[] expr, Func<int, int, bool> compareInteger, Func<bool, bool, bool> compareBool, Func<string?, string?, bool> compareStrings)
    {
        var va = Visit(expr[0]);
        var vb = Visit(expr[1]);
        if (va is JValue a && a.Type == JTokenType.Integer && vb is JValue b && b.Type == JTokenType.Integer)
        {
            return compareInteger(a.Value<int>(), b.Value<int>());
        }
        else if (va is JValue aBool && aBool.Type == JTokenType.Boolean && vb is JValue bBool && bBool.Type == JTokenType.Boolean)
        {
            return compareBool(aBool.Value<bool>(), bBool.Value<bool>());
        }
        else if (va is JValue aString && aString.Type == JTokenType.String && vb is JValue bString && bString.Type == JTokenType.String)
        {
            return compareStrings(aString.Value<string>(), bString.Value<string>());
        }
        throw new LogParseException($"Can't compare types {va.Type}and {vb.Type}");
    }

    public override JToken VisitEGreaterEquals(LogExpressionParser.EGreaterEqualsContext context)
    {
        return CompareIntegers(context.expr(), (a, b) => a >= b);
    }

    public override JToken VisitELessThan(LogExpressionParser.ELessThanContext context)
    {
        return CompareIntegers(context.expr(), (a, b) => a < b);
    }

    public override JToken VisitENotEquals(LogExpressionParser.ENotEqualsContext context)
    {
        return Compare(context.expr(), (a, b) => a != b, (a, b) => a != b, (a, b) => a != b);
    }

    public override JToken VisitETrue(LogExpressionParser.ETrueContext context)
    {
        return true;
    }

    public override JToken VisitEOr(LogExpressionParser.EOrContext context)
    {
        return CompareBool(context.expr(), (a, b) => a || b);
    }

    public override JToken VisitELessEquals(LogExpressionParser.ELessEqualsContext context)
    {
        return CompareIntegers(context.expr(), (a, b) => a <= b);
    }

    public override JToken VisitEAnd(LogExpressionParser.EAndContext context)
    {
        return CompareBool(context.expr(), (a, b) => a && b);
    }

    public override JToken VisitEFalse(LogExpressionParser.EFalseContext context)
    {
        return false;
    }

    public override JToken VisitEGreaterThan(LogExpressionParser.EGreaterThanContext context)
    {
        return CompareIntegers(context.expr(), (a, b) => a > b);
    }

    public override JToken VisitEEquals(LogExpressionParser.EEqualsContext context)
    {
        return Compare(context.expr(), (a, b) => a == b, (a, b) => a == b, (a, b) => a == b);
    }

    public override JToken VisitEIsNull(LogExpressionParser.EIsNullContext context)
    {
        var a = Visit(context.expr());
        return a == null || a.Type == JTokenType.Null;
    }

    public override JToken VisitEIsNotNull(LogExpressionParser.EIsNotNullContext context)
    {
        var a = Visit(context.expr());
        return a != null && a.Type != JTokenType.Null;
    }
}