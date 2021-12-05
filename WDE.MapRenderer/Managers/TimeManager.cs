using TheEngine.Interfaces;
using TheMaths;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class TimeManager
    {
        private readonly IUIManager uiManager;
        private readonly IGameProperties gameProperties;
        private static float[] Speeds = new float[] { 0.125f, 0.25f, 0.5f, 1, 2, 4, 8 };

        private int timeSpeedMultiplier = 3;
        private float minutes = 0;
        
        public Time Time { get; private set; }
        public float MinuteFraction => minutes - Time.TotalMinutes;
        
        public TimeManager(IUIManager uiManager,
            IGameProperties gameProperties)
        {
            this.uiManager = uiManager;
            this.gameProperties = gameProperties;
        }
        
        private int TimeSpeedMultiplier
        {
            get => timeSpeedMultiplier;
            set
            {
                if (value >= 0 && value < Speeds.Length)
                    timeSpeedMultiplier = value;
            }
        }

        public void Update(float delta)
        {
            if (gameProperties.CurrentTime.TotalMinutes != Time.TotalMinutes)
            {
                minutes = gameProperties.CurrentTime.TotalMinutes;
                Time = Time.FromMinutes((int)minutes);
            }

            if (gameProperties.DisableTimeFlow)
                return;

            TimeSpeedMultiplier = gameProperties.TimeSpeedMultiplier;
            
            minutes += delta / 60 * Speeds[timeSpeedMultiplier];
            if (minutes > 1440)
                minutes -= 1440;
            
            Time = Time.FromMinutes((int)minutes);
            gameProperties.CurrentTime = Time;
        }

        public void RenderGUI()
        {
            using var ui = uiManager.BeginImmediateDrawAbs(0, 0);
            ui.BeginVerticalBox(new Vector4(0, 0, 0, 0.7f), 5);
            ui.Text("calibri", $"{Time.Hour:00}:{Time.Minute:00}", 16, Vector4.One);
            ui.EndBox();
        }
    }
}