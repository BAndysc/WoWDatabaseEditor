using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DynamicData;
using SmartFormat;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Exporter
{
    public static class EventAiSerializer
    {
        public static string SerializeEvent(EventAiEvent e)
        {
            return $"@event({e.Id}, {e.Chance.Value}, {e.Flags.Value}, {e.Phases.Value}, {e.GetParameter(0).Value}, {e.GetParameter(1).Value}, {e.GetParameter(2).Value}, {e.GetParameter(3).Value}, {e.GetParameter(4).Value}, {e.GetParameter(5).Value})";
        }

        public static string SerializeAction(EventAiAction a)
        {
            return $"@action({a.Id}, {a.GetParameter(0).Value}, {a.GetParameter(1).Value}, {a.GetParameter(2).Value})";
        }

        public static string SerializeActions(IEnumerable<EventAiAction> a)
        {
            return string.Join("\n", a.Select(SerializeAction));
        }

        public static string SerializeEventsWithActions(IEnumerable<EventAiEvent> events)
        {
            StringBuilder sb = new();
            foreach (var e in events)
            {
                sb.AppendLine(SerializeEvent(e));
                foreach (var action in e.Actions)
                    sb.AppendLine(SerializeAction(action));
            }

            return sb.ToString();
        }

        private static Regex eventRegex = new Regex("@event\\(([0-9]+), ([0-9]+), ([0-9]+), ([0-9]+), ([0-9]+), ([0-9]+), ([0-9]+), ([0-9]+), ([0-9]+), ([0-9]+)\\)");
        private static Regex actionRegex = new Regex("@action\\(([0-9]+), ([0-9]+), ([0-9]+), ([0-9]+)\\)");

        public static bool TryDeserializeEvent(string str, IEventAiFactory factory, out EventAiEvent e)
        {
            e = null!;
            var m = eventRegex.Match(str);
            if (!m.Success)
                return false;

            var values = Enumerable.Range(1, m.Groups.Count - 1)
                .Select(i => m.Groups[i].Value).ToList();

            List<long> lValues = new();
            foreach (var val in values)
            {
                if (!long.TryParse(val, out var lVal))
                    return false;
                lValues.Add(lVal);
            }
            
            e = factory.EventFactory((uint)lValues[0]);
            e.Chance.Value = lValues[1];
            e.Flags.Value = lValues[2];
            e.Phases.Value = lValues[3];
            
            for (int i = 0; i < EventAiEvent.EventParamsCount; ++i)
                e.GetParameter(i).Value = lValues[4 + i];
            
            return true;
        }
        
        public static bool TryDeserializeAction(string str, IEventAiFactory factory, out EventAiAction action)
        {
            action = null!;
            var m = actionRegex.Match(str);
            if (!m.Success)
                return false;

            if (!uint.TryParse(m.Groups[1].Value, out var id))
                return false;
            
            if (!long.TryParse(m.Groups[2].Value, out var p1))
                return false;
            
            if (!long.TryParse(m.Groups[3].Value, out var p2))
                return false;
            
            if (!long.TryParse(m.Groups[4].Value, out var p3))
                return false;

            action = factory.ActionFactory(id);

            action.GetParameter(0).Value = p1;
            action.GetParameter(1).Value = p2;
            action.GetParameter(2).Value = p3;
            
            return true;
        }
    }
}