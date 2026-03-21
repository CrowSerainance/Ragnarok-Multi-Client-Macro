using System.Windows;
using System.Windows.Interop;
using Button = System.Windows.Controls.Button;
using PersonalRagnarokTool.Core.Models;
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
        var hwnd = new WindowInteropHelper(this).Handle;
        _viewModel.AttachWindowHandle(hwnd);

        _hotkeyService.Start();
        _viewModel.RefreshHotkeyPolling();
    }

    private void OnSkillBuffCatalogClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is BuffDefinition def)
            _viewModel.AddBuffFromCatalog(def, "skill");
    }

    private void OnItemBuffCatalogClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is BuffDefinition def)
            _viewModel.AddBuffFromCatalog(def, "item");
    }
}
