using System;
using System.Collections.Generic;

namespace TimeAura.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IService> Services = new();

        public static void Register<T>(T service) where T : class, IService
        {
            if (service == null)
            {
                return;
            }

            Services[typeof(T)] = service;
        }

        public static bool TryGet<T>(out T service) where T : class, IService
        {
            if (Services.TryGetValue(typeof(T), out var value))
            {
                service = value as T;
                return service != null;
            }

            service = null;
            return false;
        }

        public static T Get<T>() where T : class, IService
        {
            if (TryGet<T>(out var service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service not registered: {typeof(T).Name}");
        }

        public static void Clear()
        {
            Services.Clear();
        }
    }
}
