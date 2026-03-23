using PersonalRagnarokTool.Core.Geometry;

namespace PersonalRagnarokTool.Tests.Geometry;

public sealed class CellMathTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(10)]
    public void SampleRandomCellOffset_StaysWithinRadius(int radius)
    {
        var random = new Random(42);
        int maxPixelDistance = radius * CellMath.PixelsPerCell;

        for (int i = 0; i < 200; i++)
        {
            var offset = CellMath.SampleRandomCellOffset(radius, random);
            double distance = Math.Sqrt(offset.X * offset.X + offset.Y * offset.Y);
            Assert.True(distance <= maxPixelDistance + CellMath.PixelsPerCell,
                $"Offset ({offset.X}, {offset.Y}) distance {distance:F1} exceeds max {maxPixelDistance}");
        }
    }

    [Fact]
    public void CenterOf_ReturnsMiddle()
    {
        var center = CellMath.CenterOf(1024, 768);
        Assert.Equal(512, center.X);
        Assert.Equal(384, center.Y);
    }

    [Fact]
    public void ApplyOffset_AddsCorrectly()
    {
        var center = new PixelPoint(500, 400);
        var offset = new PixelPoint(96, -64);
        var result = CellMath.ApplyOffset(center, offset);
        Assert.Equal(596, result.X);
        Assert.Equal(336, result.Y);
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(5, 5)]
    [InlineData(6, 5)]
    [InlineData(7, 8)]
    [InlineData(8, 8)]
    [InlineData(9, 8)]
    [InlineData(10, 10)]
    [InlineData(100, 10)]
    public void ClampRadius_SnapsToNearest(int input, int expected)
    {
        Assert.Equal(expected, CellMath.ClampRadius(input));
    }
}
