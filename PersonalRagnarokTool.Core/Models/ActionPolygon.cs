using System.Collections.ObjectModel;
using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class ActionPolygon : ObservableObject
{
    private bool _isClosed;

    public ObservableCollection<NormalizedPoint> Vertices { get; set; } = new();

    public bool IsClosed
    {
        get => _isClosed;
        set => SetProperty(ref _isClosed, value);
    }

    public bool IsReady => IsClosed && Vertices.Count >= 3;
}
