using UnityEngine;

public enum StoryEventChannel
{
    Diary,
    News,
    Message
}

/// <summary>
/// Định nghĩa 1 story event unlock theo ngày.
/// </summary>
[CreateAssetMenu(menuName = "Data/Narrative/Story Event", fileName = "StoryEvent_")]
public class StoryEventData : ScriptableObject
{
    [Tooltip("ID duy nhất cho event này")]
    public string id;

    [Tooltip("Key localization cho tiêu đề")]
    public string titleKey;

    [Tooltip("Key localization cho nội dung")]
    public string bodyKey;

    [Tooltip("Ngày unlock (1 = ngày đầu tiên)")]
    public int triggerDay = 1;

    [Tooltip("Kênh hiển thị event")]
    public StoryEventChannel channel = StoryEventChannel.Diary;

    [Tooltip("Có hiển thị notification khi unlock không")]
    public bool showNotification = true;
}
