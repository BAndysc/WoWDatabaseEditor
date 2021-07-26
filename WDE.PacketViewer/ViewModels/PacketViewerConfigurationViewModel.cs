using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.PacketViewer.PacketParserIntegration;
using WDE.PacketViewer.Settings;

namespace WDE.PacketViewer.ViewModels
{
    [AutoRegister]
    public class PacketViewerConfigurationViewModel : ObservableBase, IConfigurable
    {
        private readonly IPacketViewerSettings settings;
        private bool alwaysSplitUpdates;
        private bool wrapLines;
        private bool isModified;

        public bool AlwaysSplitUpdates
        {
            get => alwaysSplitUpdates;
            set => SetProperty(ref alwaysSplitUpdates, value);
        }

        public bool WrapLines
        {
            get => wrapLines;
            set => SetProperty(ref wrapLines, value);
        }
        
        public INativeTextDocument DefaultFilterText { get; }

        public ObservableCollection<ParserSettingViewModel> ParserSettings { get; } = new();

        public PacketViewerConfigurationViewModel(IPacketViewerSettings settings, INativeTextDocument nativeText)
        {
            this.settings = settings;
            wrapLines = settings.Settings.WrapLines;
            alwaysSplitUpdates = settings.Settings.AlwaysSplitUpdates;
            DefaultFilterText = nativeText;
            DefaultFilterText.FromString(settings.Settings.DefaultFilter ?? "");

            AutoDispose(nativeText.Length.SubscribeAction(_ => IsModified = true));
            
            foreach (var property in settings.Settings.Parser.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<ParserConfigEntryAttribute>();
                if (attribute == null)
                    continue;
                
                var isBool = property.GetCustomAttribute<ParserIsBoolConfigAttribute>();

                ParserSettingViewModel setting =
                    isBool == null
                        ? new ParserStringSettingViewModel(attribute.FriendlyName, property.Name,
                            (string)property.GetValue(settings.Settings.Parser)!,
                            attribute.Help)
                        : new ParserBoolSettingViewModel(attribute.FriendlyName, property.Name,
                            (string)property.GetValue(settings.Settings.Parser)!,
                            attribute.Help);
                ParserSettings.Add(setting);
                AutoDispose(setting.ToObservable(t => t.StringValue).SubscribeAction(_ => IsModified = true));
            }

            Save = new DelegateCommand(() =>
            {
                var parser = ParserConfiguration.Defaults;
                foreach (var prop in ParserSettings)
                    prop.Update(ref parser);
                var defaultFilter = DefaultFilterText.ToString();
                settings.Settings = new PacketViewerSettingsData()
                {
                    AlwaysSplitUpdates = AlwaysSplitUpdates,
                    WrapLines = WrapLines,
                    DefaultFilter = string.IsNullOrEmpty(defaultFilter) ? null : defaultFilter,
                    Parser = parser
                };
                IsModified = false;
            });
            On(() => WrapLines, _ => IsModified = true);
            On(() => AlwaysSplitUpdates, _ => IsModified = true);

            IsModified = false;
        }

        public bool IsModified
        {
            get => isModified;
            set => SetProperty(ref isModified, value);
        }

        public ICommand Save { get; }
        public string Name => "Packet viewer";
        public string? ShortDescription =>
            "WoW Database Editor has builtin integration with TrinityCore's Packet Parser.";
        public bool IsRestartRequired => false;
    }
    
    public abstract class ParserSettingViewModel : BindableBase
    {
        public ParserSettingViewModel(string name, string propertyName, string? help)
        {
            Name = name;
            PropertyName = propertyName;
            Help = help;
        }

        public string Name { get; }
        public string PropertyName { get; }
        public string? Help { get; }
        public abstract string StringValue { get; }
        
        public void Update(ref ParserConfiguration parser)
        {
            object reference = parser;
            parser.GetType().GetProperty(PropertyName)!.SetValue(reference, StringValue);
            parser = (ParserConfiguration)reference;
        }
    }
    
    public class ParserStringSettingViewModel : ParserSettingViewModel
    {
        public ParserStringSettingViewModel(string name, string propertyName, string value, string? help) : base(name, propertyName, help)
        {
            this.value = value;
        }
        
        private string value;

        public string Value
        {
            get => value;
            set
            {
                SetProperty(ref this.value, value);
                RaisePropertyChanged(nameof(StringValue));
            }
        }

        public override string StringValue => Value;
    }
    
    public class ParserBoolSettingViewModel : ParserSettingViewModel
    {
        private bool value;

        public ParserBoolSettingViewModel(string name, string propertyName, string value, string? help) : base(name, propertyName, help)
        {
            this.value = value == "true";
        }

        public bool Value
        {
            get => value;
            set 
            {
                SetProperty(ref this.value, value);
                RaisePropertyChanged(nameof(StringValue));
            }
        }

        public override string StringValue => Value ? "true" : "false";
    }
}