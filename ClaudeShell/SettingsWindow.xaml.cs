using System.Windows;
using System.Windows.Input;

namespace ClaudeShell;

public partial class SettingsWindow : Window
{
    readonly Func<HotKeySetting, bool> _apply;
    readonly Action _openFolder;
    HotKeySetting _pending;

    public SettingsWindow(HotKeySetting current, Func<HotKeySetting, bool> apply, Action openFolder)
    {
        _pending = current;
        _apply = apply;
        _openFolder = openFolder;

        InitializeComponent();

        ShortcutBox.Text = current.Display;
    }

    void OnShortcutKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.None or Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
        {
            return;
        }

        if (key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
        {
            Close();

            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.None)
        {
            ShowError("Add Ctrl, Shift, Alt or Win to the shortcut.");

            return;
        }

        _pending = new HotKeySetting(Keyboard.Modifiers, key);
        ShortcutBox.Text = _pending.Display;
        ErrorText.Visibility = Visibility.Collapsed;
    }

    void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    void OnOpenFolder(object sender, RoutedEventArgs e) => _openFolder();

    void OnSave(object sender, RoutedEventArgs e)
    {
        if (_apply(_pending))
        {
            Close();

            return;
        }

        ShowError($"{_pending.Display} is already used by another app. Try a different shortcut.");
    }

    void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
