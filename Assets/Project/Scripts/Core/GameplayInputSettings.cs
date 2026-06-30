using UnityEngine;

public enum GameplayInputAction
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    PrimaryAction,
    SecondaryAction
}

public static class GameplayInputSettings
{
    public const string InteractKeyPref = "settings_input_interact_key";
    public const string MoveUpKeyPref = "settings_input_move_up_key";
    public const string MoveDownKeyPref = "settings_input_move_down_key";
    public const string MoveLeftKeyPref = "settings_input_move_left_key";
    public const string MoveRightKeyPref = "settings_input_move_right_key";
    public const string PrimaryActionKeyPref = "settings_input_primary_action_key";
    public const string SecondaryActionKeyPref = InteractKeyPref;

    public static KeyCode GetInteractKey(KeyCode fallback = KeyCode.E)
    {
        return GetKey(GameplayInputAction.SecondaryAction, fallback);
    }

    public static void SetInteractKey(KeyCode key)
    {
        SetKey(GameplayInputAction.SecondaryAction, key);
    }

    public static KeyCode GetKey(GameplayInputAction action)
    {
        return GetKey(action, GetDefaultKey(action));
    }

    public static KeyCode GetKey(GameplayInputAction action, KeyCode fallback)
    {
        string saved = PlayerPrefs.GetString(GetPrefKey(action), string.Empty);
        if (string.IsNullOrWhiteSpace(saved))
            return fallback;

        return System.Enum.TryParse(saved, out KeyCode key) ? key : fallback;
    }

    public static void SetKey(GameplayInputAction action, KeyCode key)
    {
        if (key == KeyCode.None)
            return;

        PlayerPrefs.SetString(GetPrefKey(action), key.ToString());
        PlayerPrefs.Save();
    }

    public static void ResetDefaults()
    {
        foreach (GameplayInputAction action in System.Enum.GetValues(typeof(GameplayInputAction)))
            PlayerPrefs.SetString(GetPrefKey(action), GetDefaultKey(action).ToString());

        PlayerPrefs.Save();
    }

    public static Vector2 ReadMovementVector(bool includeArrowFallback = true)
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(GetKey(GameplayInputAction.MoveLeft))) horizontal -= 1f;
        if (Input.GetKey(GetKey(GameplayInputAction.MoveRight))) horizontal += 1f;
        if (Input.GetKey(GetKey(GameplayInputAction.MoveDown))) vertical -= 1f;
        if (Input.GetKey(GetKey(GameplayInputAction.MoveUp))) vertical += 1f;

        if (includeArrowFallback)
        {
            if (Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
            if (Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
        }

        return new Vector2(Mathf.Clamp(horizontal, -1f, 1f), Mathf.Clamp(vertical, -1f, 1f)).normalized;
    }

    public static bool IsPrimaryActionDown()
    {
        return Input.GetKeyDown(GetKey(GameplayInputAction.PrimaryAction)) || Input.GetMouseButtonDown(0);
    }

    public static bool IsSecondaryActionDown(KeyCode fallback = KeyCode.E, bool allowRightMouse = false)
    {
        return Input.GetKeyDown(GetKey(GameplayInputAction.SecondaryAction, fallback))
            || (allowRightMouse && Input.GetMouseButtonDown(1));
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

    private static KeyCode GetDefaultKey(GameplayInputAction action)
    {
        return action switch
        {
            GameplayInputAction.MoveUp => KeyCode.W,
            GameplayInputAction.MoveDown => KeyCode.S,
            GameplayInputAction.MoveLeft => KeyCode.A,
            GameplayInputAction.MoveRight => KeyCode.D,
            GameplayInputAction.PrimaryAction => KeyCode.Space,
            GameplayInputAction.SecondaryAction => KeyCode.E,
            _ => KeyCode.None
        };
    }

    private static string GetPrefKey(GameplayInputAction action)
    {
        return action switch
        {
            GameplayInputAction.MoveUp => MoveUpKeyPref,
            GameplayInputAction.MoveDown => MoveDownKeyPref,
            GameplayInputAction.MoveLeft => MoveLeftKeyPref,
            GameplayInputAction.MoveRight => MoveRightKeyPref,
            GameplayInputAction.PrimaryAction => PrimaryActionKeyPref,
            GameplayInputAction.SecondaryAction => SecondaryActionKeyPref,
            _ => InteractKeyPref
        };
    }
}
