using System;
using System.Collections.Generic;

public sealed class InteractionContext
{
    private readonly List<InteractionOptionRuntime> options = new();

    public EntityRuntime Interactor { get; }
    public EntityRuntime Target { get; }
    public bool HasOptions => options.Count > 0;

    public InteractionContext(EntityRuntime interactor, EntityRuntime target)
    {
        Interactor = interactor;
        Target = target;
    }

    public void AddOption(string id, string textKey, int priority, Action execute)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (string.IsNullOrWhiteSpace(textKey)) return;
        if (execute == null) return;

        options.Add(new InteractionOptionRuntime(id, textKey, priority, execute));
    }

    public IReadOnlyList<InteractionOptionRuntime> GetOptions()
    {
        options.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        return options;
    }
}

public sealed class InteractionOptionRuntime
{
    public string Id { get; }
    public string TextKey { get; }
    public int Priority { get; }
    public Action Execute { get; }

    public InteractionOptionRuntime(string id, string textKey, int priority, Action execute)
    {
        Id = id;
        TextKey = textKey;
        Priority = priority;
        Execute = execute;
    }
}
