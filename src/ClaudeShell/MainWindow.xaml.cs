using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ClaudeShell;

public partial class MainWindow : Window
{
    readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.Selected) && viewModel.Selected is not null)
            {
                ProjectList.ScrollIntoView(viewModel.Selected);
            }
        };
    }

    public bool AllowClose { get; set; }

    // Deferred so a tray menu (or the taskbar) finishes taking back focus
    // first; otherwise its focus change deactivates and hides the popup.
    public void ShowPopup(string source)
    {
        Log.Info($"Open requested ({source}).");
        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, ShowNow);
    }

    void ShowNow()
    {
        try
        {
            _viewModel.Filter = "";
            _viewModel.Refresh();

            Show();
            CenterOnCursorScreen();
            ForceForeground();
            var activated = Activate();
            FilterBox.Focus();
            FilterBox.SelectAll();

            var handle = new WindowInteropHelper(this).Handle;
            GetWindowRect(handle, out var rect);
            Log.Info(
                $"Shown: visible={IsVisible}, activated={activated}, active={IsActive}, " +
                $"foregroundIsUs={GetForegroundWindow() == handle}, " +
                $"rect=({rect.Left},{rect.Top})-({rect.Right},{rect.Bottom}), projects={_viewModel.Projects.Count}.");
        }
        catch (Exception e)
        {
            Log.Error("Showing the popup failed.", e);
        }
    }

    // CenterScreen only applies on the first Show. Re-center every time on
    // the monitor under the cursor, so a monitor change while hidden can't
    // leave the popup off-screen.
    void CenterOnCursorScreen()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var area = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;

        if (!GetWindowRect(handle, out var rect))
        {
            Log.Warn($"GetWindowRect failed (error={Marshal.GetLastWin32Error()}).");
            return;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        var x = area.Left + Math.Max(0, (area.Width - width) / 2);
        var y = area.Top + Math.Max(0, (area.Height - height) / 2);

        if (!SetWindowPos(handle, IntPtr.Zero, x, y, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate))
        {
            Log.Warn($"SetWindowPos failed (error={Marshal.GetLastWin32Error()}).");
        }
    }

    // Windows refuses SetForegroundWindow from background apps. Attaching to
    // the current foreground thread's input lets us take focus anyway.
    void ForceForeground()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var foreground = GetForegroundWindow();

        if (foreground == handle)
        {
            return;
        }

        var foregroundThread = GetWindowThreadProcessId(foreground, IntPtr.Zero);
        var thisThread = GetCurrentThreadId();
        var attached = foregroundThread != 0 && foregroundThread != thisThread &&
            AttachThreadInput(foregroundThread, thisThread, true);

        var setForeground = SetForegroundWindow(handle);
        BringWindowToTop(handle);

        if (attached)
        {
            AttachThreadInput(foregroundThread, thisThread, false);
        }

        Log.Info(
            $"ForceForeground: previous=0x{foreground:X}, thread={foregroundThread}, " +
            $"attached={attached}, setForeground={setForeground}.");
    }

    const uint SwpNoSize = 0x0001;
    const uint SwpNoZOrder = 0x0004;
    const uint SwpNoActivate = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool BringWindowToTop(IntPtr hWnd);

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Log.Info("Deactivated, hiding.");
        Hide();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnClosing(e);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        switch (e.Key)
        {
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;

            case Key.Down:
                _viewModel.MoveSelection(1);
                e.Handled = true;
                break;

            case Key.Up:
                _viewModel.MoveSelection(-1);
                e.Handled = true;
                break;

            case Key.Enter:
                Launch(Keyboard.Modifiers);
                e.Handled = true;
                break;
        }
    }

    void Launch(ModifierKeys modifiers)
    {
        if (_viewModel.Selected is null)
        {
            return;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            _viewModel.OpenClaude();
        }
        else
        {
            _viewModel.OpenInExplorer();
        }

        Hide();
    }

    void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ProjectItem item })
        {
            _viewModel.Hide(item);
            FilterBox.Focus();
        }
    }

    // Clicking a row gives it focus (needed for it to select). Hand focus
    // back to the filter box so typing keeps working.
    void OnListMouseUp(object sender, MouseButtonEventArgs e) => FilterBox.Focus();

    void OnSortDropDownClosed(object? sender, EventArgs e) => FilterBox.Focus();

    void OnItemDoubleClick(object sender, MouseButtonEventArgs e)
    {
        Launch(ModifierKeys.None);
    }
}
