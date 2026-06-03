using UnityEngine;

[System.Serializable]
public class SeasonRuleModule : IModuleData
{
    [Tooltip("Bật để bỏ qua danh sách mùa và cho phép hoạt động quanh năm.")]
    public bool allowAllSeasons = true;

    [Tooltip("Danh sách mùa được phép hoạt động/trồng trọt.")]
    public Season[] allowedSeasons;

    [Tooltip("Nếu bật, item/seed không thể được dùng để đặt khi đang trái mùa.")]
    public bool blockPlacementOutOfSeason = true;

    [Tooltip("Khi đang trái mùa, entity sẽ xử lý theo cách nào.")]
    public OutOfSeasonBehavior outOfSeasonBehavior = OutOfSeasonBehavior.None;

    [Tooltip("Stage ngủ đông nếu outOfSeasonBehavior = Dormant. -1 = bỏ qua.")]
    public int dormantStageIndex = -1;

    public bool AllowsSeason(Season season)
    {
        if (allowAllSeasons)
            return true;

        if (allowedSeasons == null || allowedSeasons.Length == 0)
            return false;

        for (int i = 0; i < allowedSeasons.Length; i++)
        {
            if (allowedSeasons[i] == season)
                return true;
        }

        return false;
    }

    public override IModuleRuntime CreateRuntime()
    {
        return new SeasonRuleRuntime(this);
    }
}
