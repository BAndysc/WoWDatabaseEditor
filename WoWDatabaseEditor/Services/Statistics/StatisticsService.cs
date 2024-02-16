using System;
using WDE.Common.Factories.Http;
using WDE.Common.Services;
using WDE.Common.Services.Statistics;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Statistics
{
    [SingleInstance]
    [AutoRegister]
    public class StatisticsService : IStatisticsService
    {
        private readonly IUserSettings settings;
        public readonly DateTime? LastRun;
        public ulong RunCounter { get; init; }
        
        public StatisticsService(IUserSettings settings)
        {
            this.settings = settings;
            var data = settings.Get<StatisticsData>();
            LastRun = data.LastRun;
            RunCounter = data.RunCounter + 1;
            settings.Update(new StatisticsData()
            {
                LastRun = DateTime.Now.ToUniversalTime(),
                RunCounter = RunCounter
            });
        }
    }

    [AutoRegister]
    public class StatisticsUserAgent : IUserAgentPart
    {
        private readonly StatisticsService state;

        public StatisticsUserAgent(StatisticsService state)
        {
            this.state = state;
        }
        
        public string Part
        {
            get
            {
                if (state.LastRun == null)
                    return $"run: {state.RunCounter}, lastStart: Unknown";

                var date = state.LastRun.Value.ToString("MM/dd/yyyy HH:mm");
                return $"run: {state.RunCounter}, lastStart: {date}";
            }
        }
    }
}