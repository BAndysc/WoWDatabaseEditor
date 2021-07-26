using System;
using System.Diagnostics.CodeAnalysis;

namespace WDE.Updater.Models
{
    [ExcludeFromCodeCoverage]
    public class ChangeLogEntry
    {
        public long Version { get; set; }
        public string? VersionName { get; set; }
        public DateTime Date { get; set; }
        public string? UpdateTitle { get; set; }
        public string[]? Changes { get; set; }

        public string Title => UpdateTitle == null ? (VersionName ?? "") : $"{UpdateTitle} ({VersionName})";
    }
}