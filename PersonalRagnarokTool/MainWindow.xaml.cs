using System.Windows;
using System.Windows.Interop;
using PersonalRagnarokTool.Services;
using PersonalRagnarokTool.ViewModels;

namespace PersonalRagnarokTool;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly GlobalHotkeyService _hotkeyService;

    public MainWindow(MainViewModel viewModel, GlobalHotkeyService hotkeyService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _hotkeyService = hotkeyService;
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Provide the window handle for any future use (window targeting, etc.)
        var hwnd = new WindowInteropHelper(this).Handle;
        _viewModel.AttachWindowHandle(hwnd);

        // Start the GetAsyncKeyState polling thread (replaces RegisterHotKey)
        _hotkeyService.Start();
        _viewModel.RefreshHotkeyPolling();
    }
}
