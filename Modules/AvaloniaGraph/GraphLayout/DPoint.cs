namespace AvaloniaGraph.GraphLayout;

// Since I'm trying to allow GraphLayout to be used from various
// platforms which have different definitions of points, I make
// my own point structure here to be used by all platforms.
public struct DPoint
{
    public double X;
    public double Y;

    public DPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
}