using System.Windows;
using System.Windows.Interop;
using PersonalRagnarokTool.Services;
using PersonalRagnarokTool.ViewModels;

namespace PersonalRagnarokTool;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly GlobalHotkeyService _hotkeyService;
    private HwndSource? _hwndSource;

    public MainWindow(MainViewModel viewModel, GlobalHotkeyService hotkeyService)
    {
        _viewModel = viewModel;
        _hotkeyService = hotkeyService;
        DataContext = _viewModel;
        InitializeComponent();
        Closing += (_, _) => { };
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
}
