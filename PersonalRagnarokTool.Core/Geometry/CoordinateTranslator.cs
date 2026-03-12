using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Core.Geometry;

public static class CoordinateTranslator
{
    public static NormalizedPoint ToNormalized(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new NormalizedPoint();
        }

        return new NormalizedPoint((double)x / width, (double)y / height);
    }

    public static PixelPoint ToPixel(NormalizedPoint point, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new PixelPoint(0, 0);
        }

        var x = (int)Math.Round(point.X * width, MidpointRounding.AwayFromZero);
        var y = (int)Math.Round(point.Y * height, MidpointRounding.AwayFromZero);
        return new PixelPoint(Math.Clamp(x, 0, width), Math.Clamp(y, 0, height));
    }
}
