using System;
using WDE.Common.Services;

namespace WoWDatabaseEditorCore.Services.Statistics
{
    public struct StatisticsData : ISettings
    {
        public ulong RunCounter;
        public DateTime? LastRun;
    }
}