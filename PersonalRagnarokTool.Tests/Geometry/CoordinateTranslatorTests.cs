using PersonalRagnarokTool.Core.Geometry;
using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Tests.Geometry;

public sealed class CoordinateTranslatorTests
{
    [Fact]
    public void ToNormalized_ConvertsPixelsIntoNormalizedCoordinates()
    {
        NormalizedPoint point = CoordinateTranslator.ToNormalized(480, 270, 960, 540);

        Assert.Equal(0.5, point.X, 3);
        Assert.Equal(0.5, point.Y, 3);
    }

    [Fact]
    public void ToPixel_ConvertsNormalizedCoordinatesIntoPixels()
    {
        PixelPoint pixel = CoordinateTranslator.ToPixel(new NormalizedPoint(0.25, 0.75), 800, 600);

        Assert.Equal(200, pixel.X);
        Assert.Equal(450, pixel.Y);
    }
}
