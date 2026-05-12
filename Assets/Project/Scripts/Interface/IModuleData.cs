using UnityEngine;
[System.Serializable]
public abstract class IModuleData
{
    public abstract IModuleRuntime CreateRuntime();
}