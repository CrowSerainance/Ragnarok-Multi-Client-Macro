using System.Diagnostics;
using System.IO;
using System.Windows;
using PersonalRagnarokTool.Core.Services;
using PersonalRagnarokTool.Services;
using PersonalRagnarokTool.ViewModels;

namespace PersonalRagnarokTool;

public partial class App : System.Windows.Application
{
    private MainViewModel? _mainViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string configPath = Path.Combine(AppContext.BaseDirectory, "Config", "appconfig.json");
        var configStore = new AppConfigStore();
        var discoveryService = new ClientDiscoveryService(Process.GetCurrentProcess().Id);
        var hotkeyService = new GlobalHotkeyService();
        var inputDispatcher = new InputDispatcher();
        var turboEngine = new TurboEngine(inputDispatcher);
        var toggleService = new ToggleService();

        _mainViewModel = new MainViewModel(
            configPath, configStore, discoveryService,
            hotkeyService, inputDispatcher, turboEngine, toggleService);

        var window = new MainWindow(_mainViewModel, hotkeyService);
        this.MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainViewModel?.Dispose();
        base.OnExit(e);
    }
}
