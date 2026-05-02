using System;

[Serializable]
public class LocalizationEntry
{
    public string key;
    public string value;
}

[Serializable]
public class LocalizationFile
{
    public LocalizationEntry[] entries;
}
