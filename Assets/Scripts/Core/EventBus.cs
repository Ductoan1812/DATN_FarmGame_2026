using System;
using System.Collections.Generic;
using UnityEngine;

public class EventBus : MonoBehaviour
{
    private readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
    {
        Type eventType = typeof(TEvent);
        if (handlers.TryGetValue(eventType, out Delegate existing))
        {
            handlers[eventType] = Delegate.Combine(existing, handler);
            return;
        }

        handlers[eventType] = handler;
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
    {
        Type eventType = typeof(TEvent);
        if (!handlers.TryGetValue(eventType, out Delegate existing))
        {
            return;
        }

        Delegate updated = Delegate.Remove(existing, handler);
        if (updated == null)
        {
            handlers.Remove(eventType);
            return;
        }

        handlers[eventType] = updated;
    }

    public void Publish<TEvent>(TEvent payload) where TEvent : struct
    {
        Type eventType = typeof(TEvent);
        if (!handlers.TryGetValue(eventType, out Delegate existing))
        {
            return;
        }

        if (existing is Action<TEvent> callback)
        {
            callback.Invoke(payload);
        }
    }
}
