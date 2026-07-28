using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NUnit.Framework;
using WDE.EventAiEditor.Validation;
using WDE.EventAiEditor.Validation.Antlr;

namespace WDE.EventAiEditor.Test.Validation
{
    public class EventAiJsonRulesTests
    {
        private class StubContext : IEventAiValidationContext
        {
            public bool HasAction { get; init; } = true;
            public int EventParametersCount => 6;
            public int ActionParametersCount => 3;
            public long EventFlags { get; init; }
            public long ParameterValue { get; init; } = 1;
            public long GetEventFlags() => EventFlags;
            public long GetEventParameter(int index) => ParameterValue;
            public long GetActionParameter(int index) => ParameterValue;
        }

        private static IEnumerable<(string file, string expression)> AllJsonExpressions()
        {
            var dataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "EventAiData");
            foreach (var file in new[] { "events.json", "actions.json" })
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataDir, file)));
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("rules", out var rules))
                        foreach (var rule in rules.EnumerateArray())
                            yield return (file, rule.GetProperty("rule").GetString()!);

                    if (element.TryGetProperty("description_rules", out var descriptionRules))
                        foreach (var rule in descriptionRules.EnumerateArray())
                            if (rule.TryGetProperty("condition", out var condition))
                                yield return (file, condition.GetString()!);
                }
            }
        }

        [Test]
        public void AllRuleExpressionsInJsonDataParseAndEvaluate()
        {
            var context = new StubContext();
            foreach (var (file, expression) in AllJsonExpressions())
            {
                Assert.DoesNotThrow(() => new EventAiValidator(expression).Evaluate(context),
                    $"Expression '{expression}' from {file} failed to parse or evaluate");
            }
        }

        [Test]
        public void EventFlagsExpressionSeesDifficultyFlags()
        {
            var rule = new EventAiValidator("(event.flags & 30) != 0");
            Assert.IsTrue(rule.Evaluate(new StubContext { EventFlags = 2 }));   // EFLAG_DIFFICULTY_0
            Assert.IsTrue(rule.Evaluate(new StubContext { EventFlags = 16 }));  // EFLAG_DIFFICULTY_3
            Assert.IsFalse(rule.Evaluate(new StubContext { EventFlags = 1 })); // only EFLAG_REPEATABLE
            Assert.IsFalse(rule.Evaluate(new StubContext { EventFlags = 0 }));
        }

        [Test]
        public void ActionParamExpressionEvaluates()
        {
            var rule = new EventAiValidator("action.param(2) <= 200");
            Assert.IsTrue(rule.Evaluate(new StubContext { ParameterValue = 200 }));
            Assert.IsFalse(rule.Evaluate(new StubContext { ParameterValue = 201 }));
        }
    }
}
