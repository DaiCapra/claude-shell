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
    public void ShowPopup() =>
        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, ShowNow);

    void ShowNow()
    {
        _viewModel.Filter = "";
        _viewModel.Refresh();

        Show();
        ForceForeground();
        Activate();
        FilterBox.Focus();
        FilterBox.SelectAll();
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
        var attached = foregroundThread != thisThread &&
            AttachThreadInput(foregroundThread, thisThread, true);

        SetForegroundWindow(handle);
        BringWindowToTop(handle);

        if (attached)
        {
            AttachThreadInput(foregroundThread, thisThread, false);
        }
    }

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
