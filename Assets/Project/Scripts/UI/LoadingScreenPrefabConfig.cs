using UnityEngine;

[CreateAssetMenu(fileName = "LoadingScreenPrefabConfig", menuName = "DATN/UI/Loading Screen Prefab Config")]
public class LoadingScreenPrefabConfig : ScriptableObject
{
    [SerializeField] private LoadingScreenView prefab;

    public LoadingScreenView Prefab => prefab;
}
