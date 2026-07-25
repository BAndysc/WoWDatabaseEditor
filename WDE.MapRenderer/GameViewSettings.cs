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
                ViewDistanceModifier = 32
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

        public bool DisableShadows
        {
            get => current.DisableShadows;
            set
            {
                current.DisableShadows = value;
                settings.Update(current);
            }
        }

        public bool DontLoadDoodads
        {
            get => current.DontLoadDoodads;
            set
            {
                current.DontLoadDoodads = value;
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

        public bool ShowStatusIcons
        {
            get => current.ShowStatusIcons;
            set
            {
                current.ShowStatusIcons = value;
                settings.Update(current);
            }
        }

        public uint StatusIconsHiddenMask
        {
            get => current.StatusIconsHiddenMask;
            set
            {
                current.StatusIconsHiddenMask = value;
                settings.Update(current);
            }
        }

        public bool VSync
        {
            get => current.VSync;
            set
            {
                current.VSync = value;
                settings.Update(current);
            }
        }

        /// <summary>true = ProperTheEnginePanel (renders through the Avalonia compositor);
        /// false = NativeTheEnginePanel (a native child window). Read when a 3D view opens.</summary>
        public bool UseCompositionPanel
        {
            get => current.UseCompositionPanel;
            set
            {
                current.UseCompositionPanel = value;
                settings.Update(current);
            }
        }

        public struct Data : ISettings
        {
            public bool OverrideLighting;
            public bool DisableShadows;
            // skip loading WMO interior doodads (applied when a 3D view opens)
            public bool DontLoadDoodads;
            public bool DisableTimeFlow;
            public int TimeSpeedMultiplier;
            public bool ShowGrid;
            public int TextureQuality;
            public int CurrentTime;
            public float ViewDistanceModifier;

            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool ShowAreaTriggers;

            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool ShowStatusIcons;

            // the HIDDEN icon set (default 0 = everything visible, incl. icons added in the future)
            public uint StatusIconsHiddenMask;

            public bool UseCompositionPanel;

            // only honored by the native panel (a real Vulkan swapchain: Fifo vs Immediate);
            // the composition panel is compositor-paced, i.e. always vsynced
            public bool VSync;
        }
    }
}