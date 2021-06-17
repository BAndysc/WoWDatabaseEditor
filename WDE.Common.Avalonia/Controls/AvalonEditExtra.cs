using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Avalonia;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaStyles;

namespace WDE.Common.Avalonia.Controls
{
    public class AvalonEditExtra
    {
        public static readonly AttachedProperty<string> SyntaxProperty = AvaloniaProperty.RegisterAttached<IAvaloniaObject, string>("Syntax", typeof(AvalonEditExtra));

        public static string GetSyntax(IAvaloniaObject obj)
        {
            return obj.GetValue(SyntaxProperty);
        }

        public static void SetSyntax(IAvaloniaObject obj, string value)
        {
            obj.SetValue(SyntaxProperty, value);
            if (obj is TextEditor textEditor)
                textEditor.SyntaxHighlighting = AvaloniaEditSyntaxManager.Instance.GetDefinition(value);
        }
    }

    public class AvaloniaEditSyntaxManager
    {
        public static AvaloniaEditSyntaxManager Instance { get; } = new();

        private Dictionary<string, IHighlightingDefinition?> definitions = new();
        
        public IHighlightingDefinition? GetDefinition(string path)
        {
            if (definitions.TryGetValue(path, out var definition))
                return definition;

            var originalPath = path;
            if (SystemTheme.EffectiveThemeIsDark)
            {
                var darkDefinition = Path.ChangeExtension(path, null) + "Dark.xml";
                if (File.Exists(darkDefinition))
                    path = darkDefinition;
            }

            if (!File.Exists(path))
            {
                Console.WriteLine("Cannot find syntax definition at path " + path);
                throw new Exception("Cannot find syntax definition at path " + path);
            }

            using FileStream file = File.Open(path, FileMode.Open);
            using XmlTextReader reader = new XmlTextReader(file);
            IHighlightingDefinition? syntax = null;
            try
            {
                syntax = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (syntax == null)
                Console.WriteLine("Invalid syntax in path " + path);

            definitions[originalPath] = syntax;
            return syntax;
        }
    }
}