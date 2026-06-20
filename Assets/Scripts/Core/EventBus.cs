using System;
using System.Collections.Generic;

namespace TimeAura.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> Handlers = new();

        public static IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var type = typeof(T);
            if (!Handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                Handlers[type] = list;
            }

            list.Add(handler);
            return new Subscription<T>(handler);
        }

        public static void Publish<T>(T payload)
        {
            if (!Handlers.TryGetValue(typeof(T), out var list))
            {
                return;
            }

            var snapshot = list.ToArray();
            foreach (var handler in snapshot)
            {
                if (handler is Action<T> action)
                {
                    try
                    {
                        action.Invoke(payload);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[EventBus] Error in handler for {typeof(T).Name}: {ex}");
                    }
                }
            }
        }

        private sealed class Subscription<T> : IDisposable
        {
            private readonly Action<T> handler;
            private bool disposed;

            public Subscription(Action<T> handler)
            {
                this.handler = handler;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                var type = typeof(T);
                if (Handlers.TryGetValue(type, out var list))
                {
                    list.Remove(handler);
                    if (list.Count == 0)
                    {
                        Handlers.Remove(type);
                    }
                }
            }
        }
    }
}
