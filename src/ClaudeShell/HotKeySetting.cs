using System.Windows.Input;

namespace ClaudeShell;

public record HotKeySetting(ModifierKeys Modifiers, Key Key)
{
    public static HotKeySetting Default { get; } = new(ModifierKeys.Control | ModifierKeys.Shift, Key.Space);

    public string Display
    {
        get
        {
            var parts = new List<string>();

            if (Modifiers.HasFlag(ModifierKeys.Control))
            {
                parts.Add("Ctrl");
            }

            if (Modifiers.HasFlag(ModifierKeys.Shift))
            {
                parts.Add("Shift");
            }

            if (Modifiers.HasFlag(ModifierKeys.Alt))
            {
                parts.Add("Alt");
            }

            if (Modifiers.HasFlag(ModifierKeys.Windows))
            {
                parts.Add("Win");
            }

            parts.Add(Key.ToString());

            return string.Join("+", parts);
        }
    }
}
