using UnityEngine;

public static class GameplayInputSettings
{
    public const string InteractKeyPref = "settings_input_interact_key";

    public static KeyCode GetInteractKey(KeyCode fallback = KeyCode.E)
    {
        string saved = PlayerPrefs.GetString(InteractKeyPref, string.Empty);
        if (string.IsNullOrWhiteSpace(saved))
            return fallback;

        return System.Enum.TryParse(saved, out KeyCode key) ? key : fallback;
    }

    public static void SetInteractKey(KeyCode key)
    {
        if (key == KeyCode.None)
            return;

        PlayerPrefs.SetString(InteractKeyPref, key.ToString());
        PlayerPrefs.Save();
    }

    public static string FormatKey(KeyCode key)
    {
        return key switch
        {
            KeyCode.Space => "Space",
            KeyCode.Return => "Enter",
            KeyCode.LeftShift => "L-Shift",
            KeyCode.RightShift => "R-Shift",
            KeyCode.LeftControl => "L-Ctrl",
            KeyCode.RightControl => "R-Ctrl",
            KeyCode.LeftAlt => "L-Alt",
            KeyCode.RightAlt => "R-Alt",
            _ => key.ToString()
        };
    }
}
