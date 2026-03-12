using System.Windows.Media;

namespace PersonalRagnarokTool.Services;

public sealed class ClientPreviewSnapshot
{
    public ImageSource? Image { get; init; }

    public int ClientWidth { get; init; }

    public int ClientHeight { get; init; }

    public string Status { get; init; } = string.Empty;
}
