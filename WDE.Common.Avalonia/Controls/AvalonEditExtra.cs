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
        }
        
        
        public static readonly AttachedProperty<string> SyntaxExtensionProperty = AvaloniaProperty.RegisterAttached<IAvaloniaObject, string>("SyntaxExtension", typeof(AvalonEditExtra));

        public static string GetSyntaxExtension(IAvaloniaObject obj)
        {
            return obj.GetValue(SyntaxExtensionProperty);
        }

        public static void SetSyntaxExtension(IAvaloniaObject obj, string value)
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
                editor.SyntaxHighlighting = AvaloniaEditSyntaxManager.Instance.GetDefinition((string)args.NewValue!);
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

        public IHighlightingDefinition? GetDefinitionForExtension(string extension)
        {
            if (definitionsByExtension.TryGetValue(extension, out var definition))
                return definition;

            var originalExtension = extension;
            var path = $"Resources/Syntax/{extension}.xml";
            
            definitionsByExtension[originalExtension] = GetDefinition(path);
            return definitionsByExtension[originalExtension];
        }
    }
}