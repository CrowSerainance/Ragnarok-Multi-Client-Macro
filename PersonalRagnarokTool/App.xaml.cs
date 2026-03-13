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
        var attachmentService = new ProcessAttachmentService();
        var memoryService = new MemoryService(attachmentService);
        var bindingService = new ClientBindingService(discoveryService, attachmentService);
        var previewService = new ClientPreviewService(bindingService);
        var hotkeyService = new GlobalHotkeyService();
        var macroExecutor = new MacroExecutor(bindingService, new BackgroundInputDispatcher(memoryService, attachmentService), new HotkeyRouter());

        _mainViewModel = new MainViewModel(
            configPath,
            configStore,
            discoveryService,
            bindingService,
            previewService,
            hotkeyService,
            macroExecutor);

        var window = new MainWindow(_mainViewModel, hotkeyService);
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainViewModel?.Dispose();
        base.OnExit(e);
    }
}
