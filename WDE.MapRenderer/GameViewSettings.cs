using System.ComponentModel;
using Newtonsoft.Json;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.MapRenderer
{
    [UniqueProvider]
    [SingleInstance]
    public class GameViewSettings
    {
        private readonly IUserSettings settings;
        private Data current;

        public GameViewSettings(IUserSettings settings)
        {
            this.settings = settings;
            current = settings.Get<Data>(new Data()
            {
                OverrideLighting = false,
                DisableTimeFlow = false,
                TimeSpeedMultiplier = 3,
                ShowGrid = true,
                CurrentTime = 360,
                ViewDistanceModifier = 6
            });
        }
        
        public bool OverrideLighting
        {
            get => current.OverrideLighting;
            set
            {
                current.OverrideLighting = value;
                settings.Update(current);
            }
        }
        
        public bool DisableTimeFlow
        {
            get => current.DisableTimeFlow;
            set
            {
                current.DisableTimeFlow = value;
                settings.Update(current);
            }
        }

        public bool ShowAreaTriggers
        {
            get => current.ShowAreaTriggers;
            set
            {
                current.ShowAreaTriggers = value;
                settings.Update(current);
            }
        }
        
        public int TimeSpeedMultiplier
        {
            get => current.TimeSpeedMultiplier;
            set
            {
                current.TimeSpeedMultiplier = value;
                settings.Update(current);
            }
        }
        
        public int CurrentTime
        {
            get => current.CurrentTime;
            set
            {
                current.CurrentTime = value;
                settings.Update(current);
            }
        }
        
        public bool ShowGrid
        {
            get => current.ShowGrid;
            set
            {
                current.ShowGrid = value;
                settings.Update(current);
            }
        }

        public int TextureQuality
        {
            get => current.TextureQuality;
            set
            {
                current.TextureQuality = value;
                settings.Update(current);
            }
        }
        
        public float ViewDistanceModifier
        {
            get => current.ViewDistanceModifier;
            set
            {
                current.ViewDistanceModifier = value;
                settings.Update(current);
            }
        }
        
        public struct Data : ISettings
        {
            public bool OverrideLighting;
            public bool DisableTimeFlow;
            public int TimeSpeedMultiplier;
            public bool ShowGrid;
            public int TextureQuality;
            public int CurrentTime;
            public float ViewDistanceModifier;
            
            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool ShowAreaTriggers;
        }
    }
}