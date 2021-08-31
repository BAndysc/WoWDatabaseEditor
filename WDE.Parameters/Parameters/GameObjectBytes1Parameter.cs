using WDE.Common.Parameters;

namespace WDE.Parameters.Parameters
{
    public class GameObjectBytes1Parameter : Parameter
    {
        public override string ToString(long key)
        {
            var goState = key & 0xFF;
            var type = (key >> 8) & 0xFF;
            var artKit = (key >> 16) & 0xFF;
            var animProgress = (key >> 24) & 0xFF;

            return $"State: {goState}, Type: {type}, Art Kit: {artKit}, Anim Progress: {animProgress}";
        }
    }
}