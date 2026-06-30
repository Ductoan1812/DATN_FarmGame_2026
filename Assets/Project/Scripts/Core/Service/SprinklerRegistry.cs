using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks all active sprinklers in the scene.
/// GameManager ticks registered sprinklers after new-day water has been reset.
/// </summary>
public class SprinklerRegistry
{
    private readonly List<SprinklerRuntime> _sprinklers = new();

    public void Register(SprinklerRuntime sprinkler)
    {
        if (sprinkler != null && !_sprinklers.Contains(sprinkler))
            _sprinklers.Add(sprinkler);
    }

    public void Unregister(SprinklerRuntime sprinkler)
    {
        _sprinklers.Remove(sprinkler);
    }

    public void TickAll()
    {
        for (int i = _sprinklers.Count - 1; i >= 0; i--)
        {
            var sprinkler = _sprinklers[i];
            if (sprinkler == null || !sprinkler.IsAlive)
            {
                _sprinklers.RemoveAt(i);
                continue;
            }

            sprinkler.Tick();
        }
    }

    public void Clear() => _sprinklers.Clear();
}
