using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Avalonia;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaStyles;
using WDE.Common.Services;
using WDE.Common.Utils;

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
                var extension = (string)args.NewValue!;
                if (AvaloniaEditSyntaxManager.Instance.GetCachedDefinition(extension) is { } definition)
                    editor.SyntaxHighlighting = definition;
                else
                {
                    async Task LoadAsync()
                    {
                        editor.SyntaxHighlighting = await AvaloniaEditSyntaxManager.Instance.GetDefinitionForExtension(extension);
                    }

                    LoadAsync().ListenWarnings();
                }
            });
            SyntaxProperty.Changed.AddClassHandler<TextEditor>((editor, args) =>
            {
                try
                {
                    var path = (string)args.NewValue!;
                    if (AvaloniaEditSyntaxManager.Instance.GetCachedDefinition(path) is { } definition)
                        editor.SyntaxHighlighting = definition;
                    else
                    {
                        async Task LoadAsync()
                        {
                            editor.SyntaxHighlighting = await AvaloniaEditSyntaxManager.Instance.GetDefinition(path);
                        }

                        LoadAsync().ListenErrors();
                    }
                }
                catch (Exception e)
                {
                    LOG.LogError(e);
                }
            });
        }
    }

    public class AvaloniaEditSyntaxManager
    {
        public static AvaloniaEditSyntaxManager Instance { get; } = new();
        private IRuntimeDataService? dataService;

        private Dictionary<string, IHighlightingDefinition?> definitions = new();
        private Dictionary<string, IHighlightingDefinition?> definitionsByExtension = new();

        public IHighlightingDefinition? GetCachedDefinition(string? path)
        {
            if (path != null && definitions.TryGetValue(path, out var definition))
                return definition;
            return null;
        }

        public async Task<IHighlightingDefinition?> GetDefinition(string path)
        {
            if (definitions.TryGetValue(path, out var definition))
                return definition;

            dataService ??= DI.Resolve<IRuntimeDataService>();

            var originalPath = path;
            if (SystemTheme.EffectiveThemeIsDark)
            {
                var darkDefinition = Path.ChangeExtension(path, null) + "Dark.xml";
                var darkDefinition2 = Path.ChangeExtension(path, null) + ".dark.xml";
                if (await dataService.Exists(darkDefinition))
                    path = darkDefinition;
                else if (await dataService.Exists(darkDefinition2))
                    path = darkDefinition2;
            }

            if (!await dataService.Exists(path))
            {
                throw new Exception("Cannot find syntax definition at path " + path);
            }

            IHighlightingDefinition? syntax = null;
            try
            {
                var text = await dataService.ReadAllText(path);
                using XmlTextReader reader = new XmlTextReader(new StringReader(text));
                syntax = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
            catch (Exception e)
            {
                LOG.LogWarning(e, "Invalid syntax in path " + path);
            }

            definitions[originalPath] = syntax;
            return syntax;
        }

        public async Task<IHighlightingDefinition?> GetDefinitionForExtension(string? extension)
        {
            if (extension == null)
                return null;

            if (definitionsByExtension.TryGetValue(extension, out var definition))
                return definition;

            var originalExtension = extension;
            var path = $"Resources/Syntax/{extension}.xml";

            definitionsByExtension[originalExtension] = await GetDefinition(path);
            return definitionsByExtension[originalExtension];
        }
    }
}