using System;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using WDE.Debugger.Services.Logs.LogExpressions;

namespace WDE.Debugger.Test.Services.LogExpressions;

public class LogExpressionServiceTests
{
    [Test]
    public void Parse_Identifier_ReturnsCorrectValue()
    {
        JObject root = new JObject
        {
            { "myId", 123 }
        };

        var service = new LogExpressionService();
        var result = service.Parse(root, "myId");

        Assert.AreEqual(123, result.Value<int>());
    }

    [Test]
    public void Parse_Multiplication_ReturnsCorrectResult()
    {
        JObject root = new JObject();

        var service = new LogExpressionService();
        var result = service.Parse(root, "6 * 7");

        Assert.AreEqual(42, result.Value<int>());
    }

    [Test]
    public void Parse_Division_ReturnsCorrectResult()
    {
        JObject root = new JObject();

        var service = new LogExpressionService();
        var result = service.Parse(root, "42 / 6");

        Assert.AreEqual(7, result.Value<int>());
    }

    [Test]
    public void Parse_ObjectField_ReturnsCorrectValue()
    {
        JObject root = new JObject
        {
            { "person", new JObject { { "name", "John" } } }
        };

        var service = new LogExpressionService();
        var result = service.Parse(root, "person.name");

        Assert.AreEqual("John", result.Value<string>());
    }

    [Test]
    public void Parse_ArrayPropertyAccess_ReturnsCorrectLength()
    {
        var array = new JArray { 1, 2, 3, 4, 5 };
        JObject root = new JObject
        {
            { "myArray", array }
        };

        var service = new LogExpressionService();

        // Test various ways to access array length
        var resultLength = service.Parse(root, "myArray.length");
        var resultSize = service.Parse(root, "myArray.size");
        var resultCount = service.Parse(root, "myArray.Count");

        Assert.AreEqual(array.Count, resultLength.Value<int>());
        Assert.AreEqual(array.Count, resultSize.Value<int>());
        Assert.AreEqual(array.Count, resultCount.Value<int>());
    }

    [Test]
    public void Parse_ThisIdentifier_ReturnsRootObject()
    {
        JObject root = new JObject
        {
            { "name", "RootObject" }
        };

        var service = new LogExpressionService();
        var result = service.Parse(root, "this");

        Assert.IsTrue(JToken.DeepEquals(root, result), "The 'this' identifier should return the root object.");
    }

    [Test]
    public void Parse_StringConcatenation_ReturnsConcatenatedString()
    {
        JObject root = new JObject();

        var service = new LogExpressionService();
        var result = service.Parse(root, "\"Hello, \" \"world!\"");

        Assert.AreEqual("Hello, world!", result.Value<string>());
    }

    [Test]
    public void Parse_ParenthesizedExpression_EvaluatesCorrectly()
    {
        JObject root = new JObject();
        var service = new LogExpressionService();
        var result = service.Parse(root, "(3 + 4) * 2");

        Assert.AreEqual(14, result.Value<int>());
    }

    [Test]
    public void Parse_ArrayAccess_ReturnsCorrectElement()
    {
        var array = new JArray { 1, 2, 3 };
        JObject root = new JObject { ["myArray"] = array };
        var service = new LogExpressionService();
        var result = service.Parse(root, "myArray[1]");

        Assert.AreEqual(2, result.Value<int>());
    }

    [Test]
    public void Parse_Addition_ReturnsSum()
    {
        JObject root = new JObject();
        var service = new LogExpressionService();
        var result = service.Parse(root, "5 + 7");

        Assert.AreEqual(12, result.Value<int>());
    }

    [Test]
    public void Parse_GreaterEquals_ReturnsCorrectComparisonResult()
    {
        JObject root = new JObject();
        var service = new LogExpressionService();
        var result = service.Parse(root, "5 >= 3");

        Assert.IsTrue(result.Value<bool>());
    }

    [Test]
    public void Parse_LessThan_ReturnsCorrectComparisonResult()
    {
        JObject root = new JObject();
        var service = new LogExpressionService();
        var result = service.Parse(root, "2 < 3");

        Assert.IsTrue(result.Value<bool>());
    }

    [Test]
    public void Parse_NotEquals_ReturnsCorrectComparisonResult()
    {
        JObject root = new JObject();
        var service = new LogExpressionService();
        var result = service.Parse(root, "5 != 3");

        Assert.IsTrue(result.Value<bool>());
    }

    [Test]
    public void Parse_True_ReturnsTrue()
    {
        JObject root = new JObject();
        var service = new LogExpressionService();
        var result = service.Parse(root, "true");

        Assert.IsTrue(result.Value<bool>());
    }

    [Test]
    public void Parse_LogicalOr_ReturnsCorrectResult()
    {
        JObject root = new JObject();
        var service = new LogExpressionService();
        var result = service.Parse(root, "false || true");

        Assert.IsTrue(result.Value<bool>());
    }

    [Test]
    public void Parse_LogicalAnd_ReturnsCorrectResult()
    {
        JObject root = new JObject();
        var service = new LogExpressionService();
        var result = service.Parse(root, "true && false");

        Assert.IsFalse(result.Value<bool>());
    }

    [Test]
    public void Parse_LessEquals_ReturnsCorrectComparisonResult()
    {
        JObject root = new JObject();
        var service = new LogExpressionService();
        var result = service.Parse(root, "2 <= 2");

        Assert.IsTrue(result.Value<bool>());
    }

    [Test]
    public void Parse_False_ReturnsFalse()
    {
        JObject root = new JObject();
        var service = new LogExpressionService();
        var result = service.Parse(root, "false");

        Assert.IsFalse(result.Value<bool>());
    }

    [Test]
    public void Parse_IsNull_ReturnsTrueIfNull()
    {
        JObject root = new JObject { ["value"] = null };
        var service = new LogExpressionService();
        var result = service.Parse(root, "value is null");

        Assert.IsTrue(result.Value<bool>());
    }

    [Test]
    public void Parse_IsNotNull_ReturnsFalseIfNotNull()
    {
        JObject root = new JObject { ["value"] = 123 };
        var service = new LogExpressionService();
        var result = service.Parse(root, "value is not null");

        Assert.IsTrue(result.Value<bool>());
    }

    [Test]
    public void Parse_NestedObjectFieldAccess_ReturnsCorrectValue()
    {
        // Arrange: Create a root object with nested objects
        JObject root = new JObject
        {
            ["user"] = new JObject
            {
                ["profile"] = new JObject
                {
                    ["username"] = "testUser",
                    ["age"] = 30
                },
                ["isActive"] = true
            }
        };

        var service = new LogExpressionService();

        // Act & Assert: Access a deeply nested field
        var usernameResult = service.Parse(root, "user.profile.username");
        Assert.AreEqual("testUser", usernameResult.Value<string>(), "Should correctly access a nested string field.");

        var ageResult = service.Parse(root, "user.profile.age");
        Assert.AreEqual(30, ageResult.Value<int>(), "Should correctly access a nested integer field.");

        // Act & Assert: Access a field of the first-level nested object
        var isActiveResult = service.Parse(root, "user.isActive");
        Assert.IsTrue(isActiveResult.Value<bool>(), "Should correctly access a boolean field of a nested object.");
    }

    [Test]
    public void Parse_MultiplicationBeforeAddition_EvaluatesCorrectly()
    {
        // Arrange
        JObject root = new JObject();

        var service = new LogExpressionService();

        // Act
        var result = service.Parse(root, "2 + 3 * 4");

        // Assert
        // 3 * 4 should be evaluated first, resulting in 12, then 2 + 12 equals 14
        Assert.AreEqual(14, result.Value<int>());
    }

    [Test]
    public void Parse_PropertyAccessBeforeMultiplication_EvaluatesCorrectly()
    {
        // Arrange
        JObject root = new JObject
        {
            ["value"] = 3
        };

        var service = new LogExpressionService();

        // Act
        var result = service.Parse(root, "this.value * 4 + 2");

        // Assert
        // 'value' resolves to 3, so it should be 3 * 4 first, resulting in 12, then 12 + 2 equals 14
        Assert.AreEqual(14, result.Value<int>());
    }

    [Test]
    public void Parse_PropertyAccessHasHigherPrecedence_EvaluatesCorrectly()
    {
        // Arrange
        JObject root = new JObject
        {
            ["user"] = new JObject
            {
                ["profile"] = new JObject
                {
                    ["age"] = 30
                }
            },
            ["multiplier"] = 2
        };

        var service = new LogExpressionService();

        // Act
        var result = service.Parse(root, "user.profile.age * multiplier");

        // Assert
        // The property access (user.profile.age) should be evaluated first to get 30,
        // then multiplied by the 'multiplier' property value (2), resulting in 60.
        Assert.AreEqual(60, result.Value<int>());
    }

    [Test]
    public void Parse_PropertyAccessBeforeArrayAccess_EvaluatesCorrectly()
    {
        // Arrange
        JObject root = new JObject
        {
            ["data"] = new JObject
            {
                ["numbers"] = new JArray { 1, 2, 3 }
            }
        };

        var service = new LogExpressionService();

        // Act
        var result = service.Parse(root, "data.numbers[2] * data.numbers[1]");

        // Assert
        Assert.AreEqual(6, result.Value<int>());
    }
}