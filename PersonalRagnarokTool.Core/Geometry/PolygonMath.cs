using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Core.Geometry;

public static class PolygonMath
{
    public static bool ContainsPoint(ActionPolygon polygon, NormalizedPoint point)
        => ContainsPoint(polygon.Vertices, point);

    public static bool ContainsPoint(IReadOnlyList<NormalizedPoint> vertices, NormalizedPoint point)
    {
        if (vertices.Count < 3)
        {
            return false;
        }

        var inside = false;
        for (int i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++)
        {
            var xi = vertices[i].X;
            var yi = vertices[i].Y;
            var xj = vertices[j].X;
            var yj = vertices[j].Y;

            var intersects = ((yi > point.Y) != (yj > point.Y))
                && (point.X < (xj - xi) * (point.Y - yi) / ((yj - yi) == 0 ? double.Epsilon : (yj - yi)) + xi);

            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    public static NormalizedPoint? TrySampleRandomPoint(ActionPolygon polygon, Random random, int maxAttempts = 2048)
    {
        if (!polygon.IsReady)
        {
            return null;
        }

        return TrySampleRandomPoint(polygon.Vertices, random, maxAttempts);
    }

    public static NormalizedPoint? TrySampleRandomPoint(IReadOnlyList<NormalizedPoint> vertices, Random random, int maxAttempts = 2048)
    {
        if (vertices.Count < 3)
        {
            return null;
        }

        var minX = vertices.Min(x => x.X);
        var maxX = vertices.Max(x => x.X);
        var minY = vertices.Min(y => y.Y);
        var maxY = vertices.Max(y => y.Y);

        for (int i = 0; i < maxAttempts; i++)
        {
            var candidate = new NormalizedPoint(
                minX + random.NextDouble() * (maxX - minX),
                minY + random.NextDouble() * (maxY - minY));

            if (ContainsPoint(vertices, candidate))
            {
                return candidate;
            }
        }

        return GetCentroid(vertices);
    }

    public static NormalizedPoint GetCentroid(IReadOnlyList<NormalizedPoint> vertices)
    {
        if (vertices.Count == 0)
        {
            return new NormalizedPoint();
        }

        var x = vertices.Average(v => v.X);
        var y = vertices.Average(v => v.Y);
        return new NormalizedPoint(x, y);
    }
}
