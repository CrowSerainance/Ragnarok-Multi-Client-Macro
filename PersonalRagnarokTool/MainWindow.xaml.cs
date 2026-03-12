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
    private bool _isDraggingVertex;
    private int _dragVertexIndex = -1;

    public MainWindow(MainViewModel viewModel, GlobalHotkeyService hotkeyService)
    {
        _viewModel = viewModel;
        _hotkeyService = hotkeyService;
        DataContext = _viewModel;
        InitializeComponent();

        _viewModel.PolygonChanged += (_, _) => RenderPolygon();
        _viewModel.TracePointsChanged += (_, _) => RenderPolygon();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += (_, _) => RenderPolygon();
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
        if (_viewModel.SelectedClient is null)
        {
            return;
        }

        var position = e.GetPosition(PreviewCanvas);
        if (TryHitVertex(position, out int hitIndex))
        {
            _viewModel.SelectedVertexIndex = hitIndex;
            _dragVertexIndex = hitIndex;
            _isDraggingVertex = true;
            PreviewCanvas.CaptureMouse();
        }
        else if (!_viewModel.SelectedClient.ActionPolygon.IsClosed)
        {
            _viewModel.AddPolygonVertex(position.X, position.Y);
        }
        else
        {
            _viewModel.SelectedVertexIndex = null;
        }

        RenderPolygon();
    }

    private void PreviewCanvas_OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDraggingVertex || _dragVertexIndex < 0)
        {
            return;
        }

        var position = e.GetPosition(PreviewCanvas);
        _viewModel.MovePolygonVertex(_dragVertexIndex, position.X, position.Y);
    }

    private void PreviewCanvas_OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isDraggingVertex = false;
        _dragVertexIndex = -1;
        PreviewCanvas.ReleaseMouseCapture();
    }

    private void PreviewCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderPolygon();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.PreviewWidth)
            or nameof(MainViewModel.PreviewHeight)
            or nameof(MainViewModel.SelectedClient)
            or nameof(MainViewModel.SelectedVertexIndex)
            or nameof(MainViewModel.SelectedTrace)
            or nameof(MainViewModel.IsTraceRecording))
        {
            RenderPolygon();
        }
    }

    private bool TryHitVertex(System.Windows.Point position, out int index)
    {
        index = -1;
        if (_viewModel.SelectedClient is null)
        {
            return false;
        }

        for (int i = 0; i < _viewModel.SelectedClient.ActionPolygon.Vertices.Count; i++)
        {
            System.Windows.Point vertexPoint = ToCanvasPoint(_viewModel.SelectedClient.ActionPolygon.Vertices[i]);
            if ((position - vertexPoint).Length <= 10)
            {
                index = i;
                return true;
            }
        }

        return false;
    }

    private void RenderPolygon()
    {
        PreviewCanvas.Children.Clear();
        if (_viewModel.SelectedClient is null)
        {
            return;
        }

        // --- Draw action polygon ---
        var vertices = _viewModel.SelectedClient.ActionPolygon.Vertices;
        if (vertices.Count > 0)
        {
            var pointCollection = new PointCollection(vertices.Select(ToCanvasPoint));
            if (_viewModel.SelectedClient.ActionPolygon.IsClosed && pointCollection.Count >= 3)
            {
                var polygon = new Polygon
                {
                    Points = pointCollection,
                    Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(135, 255, 232, 0)),
                    Stroke = System.Windows.Media.Brushes.Black,
                    StrokeThickness = 2,
                };
                PreviewCanvas.Children.Add(polygon);
            }
            else
            {
                var polyline = new Polyline
                {
                    Points = pointCollection,
                    Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 208, 83)),
                    StrokeThickness = 2,
                };
                PreviewCanvas.Children.Add(polyline);
            }

            for (int i = 0; i < vertices.Count; i++)
            {
                System.Windows.Point canvasPoint = ToCanvasPoint(vertices[i]);
                bool isSelected = _viewModel.SelectedVertexIndex == i;
                var ellipse = new Ellipse
                {
                    Width = isSelected ? 14 : 12,
                    Height = isSelected ? 14 : 12,
                    Fill = isSelected ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)) : System.Windows.Media.Brushes.White,
                    Stroke = System.Windows.Media.Brushes.Black,
                    StrokeThickness = 1.5,
                };
                Canvas.SetLeft(ellipse, canvasPoint.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, canvasPoint.Y - ellipse.Height / 2);
                PreviewCanvas.Children.Add(ellipse);
            }
        }

        // --- Draw trace points as numbered dots when a trace is selected ---
        var trace = _viewModel.SelectedTrace;
        if (trace is not null && trace.Points.Count > 0)
        {
            // Draw connecting line between points
            if (trace.Points.Count > 1)
            {
                var tracePoints = new PointCollection(trace.Points.Select(ToCanvasPoint));
                var traceLine = new Polyline
                {
                    Points = tracePoints,
                    Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 50, 200, 255)),
                    StrokeThickness = 1.5,
                    StrokeDashArray = new DoubleCollection(new[] { 4.0, 3.0 }),
                };
                PreviewCanvas.Children.Add(traceLine);
            }

            for (int i = 0; i < trace.Points.Count; i++)
            {
                var pt = ToCanvasPoint(trace.Points[i]);
                bool isSelected = ReferenceEquals(_viewModel.SelectedTracePoint, trace.Points[i]);

                // Outer dot
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

                // Number label
                var label = new System.Windows.Controls.TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontSize = 9,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                };
                label.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, pt.X - label.DesiredSize.Width / 2);
                Canvas.SetTop(label, pt.Y - label.DesiredSize.Height / 2);
                PreviewCanvas.Children.Add(label);
            }
        }
    }

    private System.Windows.Point ToCanvasPoint(NormalizedPoint point)
    {
        return new System.Windows.Point(point.X * _viewModel.PreviewWidth, point.Y * _viewModel.PreviewHeight);
    }
}
