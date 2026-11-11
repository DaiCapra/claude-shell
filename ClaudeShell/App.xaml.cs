using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ClaudeShell;

public partial class App : Application
{
    const string MutexName = "ClaudeShell.SingleInstance";
    static readonly IntPtr PerMonitorAwareV2 = new(-4);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    readonly SettingsStore _store = new();
    readonly IProjectLauncher _launcher = new ProcessProjectLauncher();

    AppSettings _settings = new();
    Mutex? _mutex;
    MainWindow? _window;
    SettingsWindow? _settingsWindow;
    GlobalHotKey? _hotKey;
    System.Windows.Forms.NotifyIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Must run before any window exists. Without it the process is DPI
        // unaware, so screen bounds, cursor and window rects are in different
        // coordinate spaces on monitors with different scaling, and the popup
        // lands on the wrong screen.
        SetProcessDpiAwarenessContext(PerMonitorAwareV2);

        DispatcherUnhandledException += (_, args) => Log.Error("Unhandled UI exception.", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Error("Unhandled exception.", args.ExceptionObject as Exception);

        _mutex = new Mutex(true, MutexName, out var isFirst);

        if (!isFirst)
        {
            Log.Info("Another instance is already running, exiting.");
            Shutdown();

            return;
        }

        Log.Info($"Started (version {typeof(App).Assembly.GetName().Version}, pid {Environment.ProcessId}).");
        WatchSystemEvents();

        _settings = _store.Load();

        var hidden = new HiddenProjects(_settings.Hidden);
        hidden.Changed += () =>
        {
            _settings = _settings with { Hidden = hidden.Snapshot };
            _store.Save(_settings);
        };

        var viewModel = new MainViewModel(new ProjectScanner(), _launcher, hidden: hidden);

        viewModel.SortOrder = _settings.SortOrder;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.SortOrder))
            {
                _settings = _settings with { SortOrder = viewModel.SortOrder };
                _store.Save(_settings);
            }
        };

        _window = new MainWindow(viewModel);

        var handle = new WindowInteropHelper(_window).EnsureHandle();

        _hotKey = new GlobalHotKey(handle, () => _window.ShowPopup("hotkey"));

        _tray = CreateTray();

        if (!RegisterHotKey(_settings.HotKey))
        {
            _tray.ShowBalloonTip(
                5000,
                "Claude Shell",
                $"{_settings.HotKey.Display} is taken by another app. Pick another in Settings.",
                System.Windows.Forms.ToolTipIcon.Warning);
        }
    }

    // Monitor, power and session changes are the usual suspects when the
    // popup stops appearing after a long run, so record them.
    static void WatchSystemEvents()
    {
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += (_, _) =>
            Log.Info($"Display settings changed: {string.Join(", ", System.Windows.Forms.Screen.AllScreens.Select(s => s.Bounds))}.");
        Microsoft.Win32.SystemEvents.PowerModeChanged += (_, args) => Log.Info($"Power mode changed: {args.Mode}.");
        Microsoft.Win32.SystemEvents.SessionSwitch += (_, args) => Log.Info($"Session switch: {args.Reason}.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Info($"Exiting (code {e.ApplicationExitCode}).");

        _hotKey?.Dispose();

        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }

        _mutex?.Dispose();

        base.OnExit(e);
    }

    bool RegisterHotKey(HotKeySetting hotKey) =>
        _hotKey!.Register((uint)hotKey.Modifiers, (uint)KeyInterop.VirtualKeyFromKey(hotKey.Key));

    bool ChangeHotKey(HotKeySetting hotKey)
    {
        if (!RegisterHotKey(hotKey))
        {
            return false;
        }

        _settings = _settings with { HotKey = hotKey };
        _store.Save(_settings);

        return true;
    }

    void OpenSettingsFolder()
    {
        Directory.CreateDirectory(_store.Folder);
        _launcher.OpenInExplorer(_store.Folder);
    }

    void ShowSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();

            return;
        }

        // Switch the hotkey off while settings is open, so pressing it
        // reaches the shortcut box instead of opening the popup.
        _hotKey!.Unregister();

        _settingsWindow = new SettingsWindow(_settings.HotKey, ChangeHotKey, OpenSettingsFolder);
        _settingsWindow.Closed += (_, _) =>
        {
            _settingsWindow = null;
            RegisterHotKey(_settings.HotKey);
        };
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    System.Windows.Forms.NotifyIcon CreateTray()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => _window?.ShowPopup("tray menu"));
        menu.Items.Add("Settings", null, (_, _) => ShowSettings());
        menu.Items.Add("Quit", null, (_, _) => Quit());

        var tray = new System.Windows.Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Claude Shell",
            ContextMenuStrip = menu,
            Visible = true
        };

        tray.MouseClick += (_, args) =>
        {
            if (args.Button == System.Windows.Forms.MouseButtons.Left)
            {
                _window?.ShowPopup("tray click");
            }
        };

        return tray;
    }

    static System.Drawing.Icon LoadIcon()
    {
        var resource = GetResourceStream(new Uri("pack://application:,,,/Assets/icon.ico"));

        using var stream = resource.Stream;

        return new System.Drawing.Icon(stream, System.Windows.Forms.SystemInformation.SmallIconSize);
    }

    void Quit()
    {
        if (_window is not null)
        {
            _window.AllowClose = true;
        }

        Shutdown();
    }
}
