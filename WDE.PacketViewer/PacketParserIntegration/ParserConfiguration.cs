using System;

namespace WDE.PacketViewer.PacketParserIntegration
{
    public readonly struct ParserConfiguration
    {
        public static ParserConfiguration Defaults => new()
        {
            UseDbc = "false",
            DbcPath = "dbc",
            DbcLocale = "enUS",
            DbEnabled = "false",
            DbServer = "localhost",
            DbPort = "3306",
            DbUsername = "root",
            DbPassword = "",
            DbWppDatabase = "wpp",
            DbTdbDatabase = "world",
            DbHotfixesDatabase = "hotfixes"
        };
        
        [ParserIsBoolConfigAttribute]
        [ParserConfigEntryAttribute("UseDBC", "false", "Use DBC in parser", "If you use DBC in parser, packets text output will contain DBC data names, for instance spell names or emote names. This however requires WPP compatible DBC, at the moment supported DBC: 9.0.1. All dumpers will work without this.")]
        public string UseDbc { get; init; }
        
        [ParserConfigEntryAttribute("DBCPath", "dbc", "DBC path", "If you want to use DBC, enable 'Use DBC' too.")]
        public string DbcPath { get; init; }
        
        [ParserConfigEntryAttribute("DBCLocale", "enUS", "DBC Locale")]
        public string DbcLocale { get; init; }
        
        [ParserIsBoolConfigAttribute]
        [ParserConfigEntryAttribute("DBEnabled", "false", "Enable database connection")]
        public string DbEnabled { get; init; }
        
        [ParserConfigEntryAttribute("Server", "localhost", "Database server")]
        public string DbServer { get; init; }
        
        [ParserConfigEntryAttribute("Port", "3306", "Database port")]
        public string DbPort { get; init; }
        
        [ParserConfigEntryAttribute("Username", "root", "Database username")]
        public string DbUsername { get; init; }
        
        [ParserConfigEntryAttribute("Password", "", "Database password")]
        public string DbPassword { get; init; }
        
        [ParserConfigEntryAttribute("WPPDatabase", "wpp", "WoW Packet Parser Database name", "The WPP database is used to feed additional data that WPP may use while parsing. For example, in the output text files, the spell name can be displayed next to spell ids")]
        public string DbWppDatabase { get; init; }
        
        //[ParserConfigEntryAttribute("TDBDatabase", "world", "TrinityCore Database name")]
        public string DbTdbDatabase { get; init; }
        
        //[ParserConfigEntryAttribute("HotfixesDatabase", "hotfixes", "Hotfixes database name")]
        public string DbHotfixesDatabase { get; init; }
    }

    public class ParserConfigEntryAttribute : Attribute
    {
        public ParserConfigEntryAttribute(string settingName, string defaultValue, string friendlyName, string? help = null)
        {
            SettingName = settingName;
            DefaultValue = defaultValue;
            FriendlyName = friendlyName;
            Help = help;
        }

        public string SettingName { get; }
        public string DefaultValue { get; }
        public string FriendlyName { get; }
        public string? Help { get; }
    }

    public class ParserIsBoolConfigAttribute : Attribute
    {
        public ParserIsBoolConfigAttribute()
        {
        }
    }
}