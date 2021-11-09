namespace TheEngine.Utils
{
    public unsafe struct RollingAverage
    {
        private int i;
        private fixed float buffer[32];
        private float avg;

        public float Average => avg;

        public void Add(double d) => Add((float)d);
        
        public void Add(float t)
        {
            avg += (t - buffer[i]) / 32;
            buffer[i] = t;
            i += 1;
            i %= 32;
        }
    }
}