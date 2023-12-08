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
        public static readonly AttachedProperty<string> SyntaxProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, string>("Syntax", typeof(AvalonEditExtra));

        public static string GetSyntax(AvaloniaObject obj)
        {
            return (string?)obj.GetValue(SyntaxProperty) ?? "";
        }

        public static void SetSyntax(AvaloniaObject obj, string value)
        {
            obj.SetValue(SyntaxProperty, value);
        }
        
        
        public static readonly AttachedProperty<string> SyntaxExtensionProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, string>("SyntaxExtension", typeof(AvalonEditExtra));

        public static string GetSyntaxExtension(AvaloniaObject obj)
        {
            return (string?)obj.GetValue(SyntaxExtensionProperty) ?? "";
        }

        public static void SetSyntaxExtension(AvaloniaObject obj, string value)
        {
            obj.SetValue(SyntaxExtensionProperty, value);
        }

        static AvalonEditExtra()
        {
            SyntaxExtensionProperty.Changed.AddClassHandler<TextEditor>((editor, args) =>
            {
                editor.SyntaxHighlighting = AvaloniaEditSyntaxManager.Instance.GetDefinitionForExtension((string)args.NewValue!);
            });
            SyntaxProperty.Changed.AddClassHandler<TextEditor>((editor, args) =>
            {
                try
                {
                    editor.SyntaxHighlighting = AvaloniaEditSyntaxManager.Instance.GetDefinition((string)args.NewValue!);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }
    }

    public class AvaloniaEditSyntaxManager
    {
        public static AvaloniaEditSyntaxManager Instance { get; } = new();

        private Dictionary<string, IHighlightingDefinition?> definitions = new();
        private Dictionary<string, IHighlightingDefinition?> definitionsByExtension = new();
        
        public IHighlightingDefinition? GetDefinition(string path)
        {
            if (definitions.TryGetValue(path, out var definition))
                return definition;

            var originalPath = path;
            if (SystemTheme.EffectiveThemeIsDark)
            {
                var darkDefinition = Path.ChangeExtension(path, null) + "Dark.xml";
                var darkDefinition2 = Path.ChangeExtension(path, null) + ".dark.xml";
                if (File.Exists(darkDefinition))
                    path = darkDefinition;
                else if (File.Exists(darkDefinition2))
                    path = darkDefinition2;
            }

            if (!File.Exists(path))
            {
                Console.WriteLine("Cannot find syntax definition at path " + path);
                throw new Exception("Cannot find syntax definition at path " + path);
            }

            IHighlightingDefinition? syntax = null;
            try
            {
                using FileStream file = File.Open(path, FileMode.Open);
                using XmlTextReader reader = new XmlTextReader(file);
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

        public IHighlightingDefinition? GetDefinitionForExtension(string extension)
        {
            if (definitionsByExtension.TryGetValue(extension, out var definition))
                return definition;

            var originalExtension = extension;
            var path = $"Resources/Syntax/{extension}.xml";

            try
            {
                definitionsByExtension[originalExtension] = GetDefinition(path);
                return definitionsByExtension[originalExtension];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}