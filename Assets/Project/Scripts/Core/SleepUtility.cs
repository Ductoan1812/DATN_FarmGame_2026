using UnityEngine;

public static class SleepUtility
{
    public static bool TrySleep(EntityRuntime player)
    {
        if (player == null)
            return false;

        var gm = GameManager.Instance;
        if (gm == null)
            return false;

        float maxStamina = player.stats.Get(StatType.MaxStamina);
        if (maxStamina > 0f)
            player.stats.Set(StatType.Stamina, maxStamina);

        float maxHp = player.stats.Get(StatType.MaxHp);
        if (maxHp > 0f)
            player.stats.Set(StatType.Hp, maxHp);

        gm.TimeManager?.SkipToNextDay();

        Debug.Log($"[SleepUtility] Slept successfully. Stamina={maxStamina}, HP={maxHp}. Advanced to next day.");
        return true;
    }
}
