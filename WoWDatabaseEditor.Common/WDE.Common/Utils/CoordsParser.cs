using System.Text.RegularExpressions;

namespace WDE.Common.Utils
{
    public class CoordsParser
    {
        private static Regex waypointRegex = new Regex(@"(-?\d+(?:\.\d+)?)[ ,;Yy:]{1,5}(-?\d+(?:\.\d+)?)[ ,;Zz:]{1,5}(-?\d+(?:\.\d+)?)(?:[ ,;Oo:]{1,5}(-?\d+(?:\.\d+)?))?");

        public static (float x, float y, float z, float? o)? ExtractCoords(string line)
        {
            var match = waypointRegex.Match(line);
            if (match.Success &&
                float.TryParse(match.Groups[1].Value, out var x) &&
                float.TryParse(match.Groups[2].Value, out var y) &&
                float.TryParse(match.Groups[3].Value, out var z))
            {
                float? o = null;
                if (match.Groups.Count == 5 &&
                    float.TryParse(match.Groups[4].Value, out var o_))
                    o = o_;
                return (x, y, z, o);
            }

            return null;
        }
    }
}