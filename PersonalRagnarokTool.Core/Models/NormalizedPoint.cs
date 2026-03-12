using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class NormalizedPoint : ObservableObject
{
    private double _x;
    private double _y;

    public NormalizedPoint()
    {
    }

    public NormalizedPoint(double x, double y)
    {
        _x = Clamp(x);
        _y = Clamp(y);
    }

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, Clamp(value));
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, Clamp(value));
    }

    public NormalizedPoint Clone() => new(X, Y);

    private static double Clamp(double value) => Math.Clamp(value, 0d, 1d);
}
