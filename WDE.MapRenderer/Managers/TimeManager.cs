using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class TimeManager
    {
        public TimeManager(IGameContext gameContext)
        {
            
        }

        private float minutes = 0;
        
        public Time Time { get; private set; }
        
        public void Update(float delta)
        {
            minutes += delta / 20;
            if (minutes > 1440)
                minutes -= 1440;
            
            Time = Time.FromMinutes((int)minutes);
        }
    }
}