using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using PersonalRagnarokTool.Core.Models;
using PersonalRagnarokTool.Services;
using PersonalRagnarokTool.ViewModels;

namespace PersonalRagnarokTool;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly GlobalHotkeyService _hotkeyService;
    private HwndSource? _hwndSource;
    private bool _isDraggingPoint;

    public MainWindow(MainViewModel viewModel, GlobalHotkeyService hotkeyService)
    {
        _viewModel = viewModel;
        _hotkeyService = hotkeyService;
        DataContext = _viewModel;
        InitializeComponent();
        _viewModel.TracePointsChanged += (_, _) => RenderSequenceOverlay();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += (_, _) => RenderSequenceOverlay();
        Closing += (_, _) => _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var helper = new WindowInteropHelper(this);
        _viewModel.AttachWindowHandle(helper.Handle);
        _hwndSource = HwndSource.FromHwnd(helper.Handle);
        _hwndSource?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            _hotkeyService.HandleWindowMessage(wParam);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void PreviewCanvas_OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var position = e.GetPosition(PreviewCanvas);
        if (TryHitSequencePoint(position, out var point))
        {
            _viewModel.SelectedTracePoint = point;
            _isDraggingPoint = true;
            PreviewCanvas.CaptureMouse();
        }
        else
        {
            _viewModel.AddSequencePoint(position.X, position.Y);
        }

        RenderSequenceOverlay();
    }

    private void PreviewCanvas_OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDraggingPoint || _viewModel.SelectedTracePoint is null)
        {
            return;
        }

        var position = e.GetPosition(PreviewCanvas);
        _viewModel.MoveSelectedTracePointOnPreview(position.X, position.Y);
    }

    private void PreviewCanvas_OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isDraggingPoint = false;
        PreviewCanvas.ReleaseMouseCapture();
    }

    private void PreviewCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e) => RenderSequenceOverlay();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.PreviewWidth)
            or nameof(MainViewModel.PreviewHeight)
            or nameof(MainViewModel.SelectedClient)
            or nameof(MainViewModel.SelectedTrace)
            or nameof(MainViewModel.SelectedTracePoint))
        {
            RenderSequenceOverlay();
        }
    }

    private bool TryHitSequencePoint(System.Windows.Point position, out NormalizedPoint? hitPoint)
    {
        hitPoint = null;
        var trace = _viewModel.SelectedTrace;
        if (trace is null)
        {
            return false;
        }

        foreach (var point in trace.Points)
        {
            if ((position - ToCanvasPoint(point)).Length <= 10)
            {
                hitPoint = point;
                return true;
            }
        }

        return false;
    }

    private void RenderSequenceOverlay()
    {
        PreviewCanvas.Children.Clear();
        var trace = _viewModel.SelectedTrace;
        if (trace is null || trace.Points.Count == 0)
        {
            return;
        }

        if (trace.Points.Count > 1)
        {
            PreviewCanvas.Children.Add(new Polyline
            {
                Points = new PointCollection(trace.Points.Select(ToCanvasPoint)),
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 50, 200, 255)),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection(new[] { 4.0, 3.0 }),
            });
        }

        for (int i = 0; i < trace.Points.Count; i++)
        {
            var pt = ToCanvasPoint(trace.Points[i]);
            bool isSelected = ReferenceEquals(_viewModel.SelectedTracePoint, trace.Points[i]);
            var dot = new Ellipse
            {
                Width = isSelected ? 18 : 14,
                Height = isSelected ? 18 : 14,
                Fill = isSelected
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 200, 255))
                    : new SolidColorBrush(System.Windows.Media.Color.FromArgb(220, 20, 150, 230)),
                Stroke = System.Windows.Media.Brushes.White,
                StrokeThickness = 1.5,
            };
            Canvas.SetLeft(dot, pt.X - dot.Width / 2);
            Canvas.SetTop(dot, pt.Y - dot.Height / 2);
            PreviewCanvas.Children.Add(dot);

            var label = new TextBlock
            {
                Text = (i + 1).ToString(),
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
            };
            label.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(label, pt.X - label.DesiredSize.Width / 2);
            Canvas.SetTop(label, pt.Y - label.DesiredSize.Height / 2);
            PreviewCanvas.Children.Add(label);
        }
    }

    private System.Windows.Point ToCanvasPoint(NormalizedPoint point) => new(point.X * _viewModel.PreviewWidth, point.Y * _viewModel.PreviewHeight);
}
