using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class TimeManager
    {
        private static float[] Speeds = new float[] { 0.125f, 0.25f, 0.5f, 1, 2, 4, 8 };
        public TimeManager(IGameContext gameContext)
        {
            
        }

        private int timeSpeedMultiplier = 3;
        private float minutes = 0;
        
        public Time Time { get; private set; }
        public float MinuteFraction => minutes - Time.TotalMinutes;
        public bool IsPaused { get; set; }

        public int TimeSpeedMultiplier
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
            if (IsPaused)
                return;
            
            minutes += delta / 60 * Speeds[timeSpeedMultiplier];
            if (minutes > 1440)
                minutes -= 1440;
            
            Time = Time.FromMinutes((int)minutes);
        }

        public void SetTime(Time newTime)
        {
            minutes = newTime.TotalMinutes;
            Time = Time.FromMinutes((int)minutes);
        }
    }
}