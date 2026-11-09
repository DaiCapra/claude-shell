using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

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

    public void ShowPopup()
    {
        _viewModel.Filter = "";
        _viewModel.Refresh();

        Show();
        Activate();
        FilterBox.Focus();
        FilterBox.SelectAll();
    }

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

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            _viewModel.OpenInTerminal();
        }
        else if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            _viewModel.ContinueClaude();
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
