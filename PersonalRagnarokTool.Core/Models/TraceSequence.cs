using System.Collections.ObjectModel;
using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class TraceSequence : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "Trace";
    private DateTimeOffset _lastUpdatedUtc = DateTimeOffset.UtcNow;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? "Trace" : value.Trim());
    }

    public DateTimeOffset LastUpdatedUtc
    {
        get => _lastUpdatedUtc;
        set => SetProperty(ref _lastUpdatedUtc, value);
    }

    public ObservableCollection<NormalizedPoint> Points { get; set; } = new();
}
