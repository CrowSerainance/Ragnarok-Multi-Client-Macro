using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PersonalRagnarokTool.Core.Models;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingGraphics = System.Drawing.Graphics;

namespace PersonalRagnarokTool.Services;

public sealed class ClientPreviewService
{
    private readonly ClientBindingService _bindingService;

    public ClientPreviewService(ClientBindingService bindingService)
    {
        _bindingService = bindingService;
    }

    public ClientPreviewSnapshot Capture(ClientProfile profile)
    {
        var liveWindow = _bindingService.ResolveLiveWindow(profile) ?? profile.BoundWindow;
        if (liveWindow is null || liveWindow.WindowHandle == 0)
        {
            return CreateFallbackSnapshot(960, 540, "Client is not bound.");
        }

        if (liveWindow.ClientWidth <= 0 || liveWindow.ClientHeight <= 0)
        {
            return CreateFallbackSnapshot(960, 540, "Client size is unavailable.");
        }

        try
        {
            var handle = new IntPtr(liveWindow.WindowHandle);
            using var bitmap = new DrawingBitmap(liveWindow.ClientWidth, liveWindow.ClientHeight);
            using (var graphics = DrawingGraphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    bool captured = NativeMethods.PrintWindow(handle, hdc, NativeMethods.PW_CLIENTONLY);
                    if (!captured)
                    {
                        return CreateFallbackSnapshot(
                            liveWindow.ClientWidth,
                            liveWindow.ClientHeight,
                            $"Client preview unavailable for {liveWindow.WindowTitle}. Using dimension-only canvas.");
                    }
                }
                finally
                {
                    graphics.ReleaseHdc(hdc);
                }
            }

            return new ClientPreviewSnapshot
            {
                Image = ToBitmapSource(bitmap),
                ClientWidth = liveWindow.ClientWidth,
                ClientHeight = liveWindow.ClientHeight,
                Status = $"Client preview captured from {liveWindow.WindowTitle}.",
            };
        }
        catch
        {
            return CreateFallbackSnapshot(liveWindow.ClientWidth, liveWindow.ClientHeight, "Client preview failed. Using dimension-only canvas.");
        }
    }

    private static ImageSource ToBitmapSource(DrawingBitmap bitmap)
    {
        IntPtr hBitmap = bitmap.GetHbitmap();
        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            NativeMethods.DeleteObject(hBitmap);
        }
    }

    private static ClientPreviewSnapshot CreateFallbackSnapshot(int width, int height, string status)
    {
        width = Math.Max(640, width);
        height = Math.Max(360, height);

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 28, 36));
            background.Freeze();
            context.DrawRectangle(background, null, new Rect(0, 0, width, height));

            var gridPen = new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(24, 255, 255, 255)), 1);
            gridPen.Freeze();
            for (int x = 0; x < width; x += 48)
            {
                context.DrawLine(gridPen, new System.Windows.Point(x, 0), new System.Windows.Point(x, height));
            }

            for (int y = 0; y < height; y += 48)
            {
                context.DrawLine(gridPen, new System.Windows.Point(0, y), new System.Windows.Point(width, y));
            }

            var borderPen = new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 208, 83)), 2);
            borderPen.Freeze();
            context.DrawRectangle(null, borderPen, new Rect(8, 8, width - 16, height - 16));

            var text = new FormattedText(
                $"{width} x {height}\n{status}",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"),
                24,
                System.Windows.Media.Brushes.White,
                1.25);

            context.DrawText(text, new System.Windows.Point(24, 24));
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        return new ClientPreviewSnapshot
        {
            Image = bitmap,
            ClientWidth = width,
            ClientHeight = height,
            Status = status,
        };
    }
}
