using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Core.Geometry;

public static class CellMath
{
    public const int PixelsPerCell = 32;

    public static readonly int[] AllowedRadii = { 5, 8, 10 };

    public static int ClampRadius(int radius)
    {
        int best = AllowedRadii[0];
        int bestDist = Math.Abs(radius - best);
        for (int i = 1; i < AllowedRadii.Length; i++)
        {
            int dist = Math.Abs(radius - AllowedRadii[i]);
            if (dist < bestDist)
            {
                best = AllowedRadii[i];
                bestDist = dist;
            }
        }

        return best;
    }

    public static PixelPoint SampleRandomCellOffset(int cellRadius, Random random)
    {
        int r = Math.Max(1, cellRadius);
        int dx, dy;
        do
        {
            dx = random.Next(-r, r + 1);
            dy = random.Next(-r, r + 1);
        } while (dx * dx + dy * dy > r * r);

        return new PixelPoint(dx * PixelsPerCell, dy * PixelsPerCell);
    }

    public static PixelPoint CenterOf(int clientWidth, int clientHeight)
        => new(clientWidth / 2, clientHeight / 2);

    public static PixelPoint ApplyOffset(PixelPoint center, PixelPoint offset)
        => new(center.X + offset.X, center.Y + offset.Y);
}
