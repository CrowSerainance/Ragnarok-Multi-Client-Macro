using PersonalRagnarokTool.Core.Geometry;
using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Tests.Geometry;

public sealed class PolygonMathTests
{
    [Fact]
    public void ContainsPoint_ReturnsTrueInsidePolygon()
    {
        ActionPolygon polygon = CreatePolygon();

        bool result = PolygonMath.ContainsPoint(polygon, new NormalizedPoint(0.5, 0.5));

        Assert.True(result);
    }

    [Fact]
    public void ContainsPoint_ReturnsFalseOutsidePolygon()
    {
        ActionPolygon polygon = CreatePolygon();

        bool result = PolygonMath.ContainsPoint(polygon, new NormalizedPoint(0.95, 0.95));

        Assert.False(result);
    }

    [Fact]
    public void TrySampleRandomPoint_ReturnsOnlyPointsInsidePolygon()
    {
        ActionPolygon polygon = CreatePolygon();

        for (int i = 0; i < 100; i++)
        {
            NormalizedPoint? sample = PolygonMath.TrySampleRandomPoint(polygon, new Random(i));

            Assert.NotNull(sample);
            Assert.True(PolygonMath.ContainsPoint(polygon, sample!));
        }
    }

    private static ActionPolygon CreatePolygon()
    {
        var polygon = new ActionPolygon { IsClosed = true };
        polygon.Vertices.Add(new NormalizedPoint(0.2, 0.2));
        polygon.Vertices.Add(new NormalizedPoint(0.8, 0.2));
        polygon.Vertices.Add(new NormalizedPoint(0.8, 0.8));
        polygon.Vertices.Add(new NormalizedPoint(0.2, 0.8));
        return polygon;
    }
}
