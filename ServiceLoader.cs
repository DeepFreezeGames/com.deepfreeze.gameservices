using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace GameServices
{
    public static class ServiceLoader
    {
        private const string LogPrefix = "[GAME SERVICES] ";
        private const BindingFlags MethodFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        
        private static readonly Dictionary<Type, RegisteredService> RegisteredServices = new();
        private static bool ServiceRegistryBuilt { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static async void Initialize()
        {
            if (GameServiceSettingsAsset.Settings.autoInitializeServices)
            {
                Log("Auto-initializing...");
                await InitializeAllServices();
            }
        }

        /// <summary>
        /// Finds all instances of <see cref="IGameService"/> and any methods tagged with the <see cref="RegisterServiceAttribute"/> attribute.
        /// </summary>
        private static void FetchAllServices()
        {
            //Find services and static services
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract && typeof(IGameService).IsAssignableFrom(type))
                    {
                        var serviceInstance = (IGameService)Activator.CreateInstance(type);
                        var service = new RegisteredService
                        {
                            ServiceType = type,
                            SortOrder = serviceInstance.SortOrder,
                            ServiceReference = new WeakReference<IGameService>(serviceInstance)
                        };
                        
                        RegisteredServices.TryAdd(service.ServiceType, service);
                    }

                    foreach (var method in type.GetMethods(MethodFlags))
                    {
                        if (Attribute.GetCustomAttribute(method, typeof(RegisterServiceAttribute)) is RegisterServiceAttribute attribute)
                        {
                            var service = new RegisteredService
                            {
                                ServiceType = type,
                                SortOrder = attribute.SortOrder,
                                StaticInvokeMethod = method,
                                StaticMethodAttribute = attribute
                            };

                            RegisteredServices.TryAdd(service.ServiceType, service);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Initializes all services that are registered. If the service is already running, this method will quietly
        /// skip its initialization.
        /// </summary>
        public static async Task InitializeAllServices()
        {
            Log("Initializing all services...");
            
            if (!ServiceRegistryBuilt)
            {
                FetchAllServices();
            }
            
            //Initialize service buffer and order the services by sort order
            var services = RegisteredServices.Values.ToList().OrderBy(s => s.SortOrder);
            
            foreach (var service in services)
            {
                if (IsServiceRunning(service.ServiceType))
                {
                    //Service is already running
                    continue;
                }

                await InitializeService(service);
            }
            
            Log("All services initialized");
        }

        /// <summary>
        /// Initializes a specific service. Extra care will need to be taken when using this method since the sort order
        /// will be ignored and the service will be immediately initialized if it is not already running.
        /// </summary>
        /// <param name="serviceType"></param>
        public static async Task InitializeService(Type serviceType)
        {
            if (!RegisteredServices.TryGetValue(serviceType, out var service))
            {
                LogError("Trying to initialize ");
                return;
            }

            await InitializeService(service);
        }

        private static async Task InitializeService(RegisteredService service)
        {
            if (IsServiceRunning(service.ServiceType))
            {
                LogWarning($"Trying to initialize service of type {service.ServiceType.Name} but an instance is already running");
                return;
            }
            
            if(GameServiceSettingsAsset.Settings.disabledServices.Contains(service.ServiceType.FullName))
            {
                Log($"Service ({service.ServiceType.FullName}) is disabled");
                return;
            }
                
            try
            {
                Log(service.ServiceReference != null
                    ? $"Loading instanced service {service.ServiceType.Name}..."
                    : $"Loading static service {service.ServiceType.Name}...");
                    
                //Initialize the service
                if (service.ServiceReference != null)
                {
                    if(service.ServiceReference.TryGetTarget(out var serviceReference))
                    {
                        await serviceReference.Initialize();
                    }
                }
                else
                {
                    if (service.StaticInvokeMethod.ReturnType == typeof(void))
                    {
                        service.StaticInvokeMethod.Invoke(null, null);
                    }
                    else if(service.StaticInvokeMethod.ReturnType == typeof(Task))
                    {
                        service.StaticServiceState = ServiceState.Starting;
                        
                        var serviceInitTask = (Task)service.StaticInvokeMethod.Invoke(null, null);
                        await serviceInitTask;

                        service.StaticServiceState = ServiceState.Running;
                    }
                    else
                    {
                        LogError($"Failed to initialize service of type {service.StaticInvokeMethod.ReturnType.FullName} but the return type is unsupported\nMethod: {service.ServiceType.Name}:{service.StaticInvokeMethod.Name}");
                        service.StaticServiceState = ServiceState.Error;
                    }
                }

                service.IsRunning = true;
                
                Log($"{service.ServiceType.Name} initialized successfully");
            }
            catch (Exception exception)
            {
                LogError($"Failed to initialize service of type {service.ServiceType.FullName}\nException: {exception.Message}");
                throw;
            }
        }

        /// <summary>
        /// Returns true if the given service type is registered and running
        /// </summary>
        public static bool IsServiceRunning(Type serviceType)
        {
            return RegisteredServices.TryGetValue(serviceType, out var registeredService) && registeredService.IsRunning;
        }

        /// <summary>
        /// Returns the <see cref="ServiceState"/> of the given service type.
        /// If the service could not be found, <see cref="ServiceState.Inactive"/> will be returned.
        /// </summary>
        public static ServiceState GetServiceState(Type serviceType)
        {
            return RegisteredServices.TryGetValue(serviceType, out var registeredService) 
                ? registeredService.CurrentState 
                : ServiceState.Inactive;
        }

        /// <summary>
        /// Shuts down a specific instanced game service
        /// </summary>
        public static void ShutdownService(Type serviceType)
        {
            if (RegisteredServices.TryGetValue(serviceType, out var registeredService))
            {
                InternalShutdownService(registeredService);
            }
        }
        
        /// <summary>
        /// Shuts down all instanced game services
        /// </summary>
        public static void ShutdownAllServices()
        {
            Log("Shutting down all services...");
            
            foreach (var registeredService in RegisteredServices.Values)
            {
                InternalShutdownService(registeredService);
            }
            
            Log("All services shut down successfully");
        }

        private static void InternalShutdownService(RegisteredService registeredService)
        {
            try
            {
                Log($"Shutting down {registeredService.ServiceType.Name}...");
                if (registeredService.ServiceReference.TryGetTarget(out var service))
                {
                    service.Shutdown();
                }

                RegisteredServices.Remove(registeredService.ServiceType);
                
                Log($"{registeredService.ServiceType} shut down successfully");
            }
            catch (Exception exception)
            {
                LogError($"Failed to shut down {registeredService.ServiceType}\nException: {exception}");
            }
        }

        #region LOGGING
        private static void Log(string message)
        {
            if (!GameServiceSettingsAsset.Settings.logMessages)
            {
                return;
            }
            
            Debug.Log($"{LogPrefix}{message}");
        }

        private static void LogWarning(string message)
        {
            if (!GameServiceSettingsAsset.Settings.logWarnings)
            {
                return;
            }
            
            Debug.LogWarning($"{LogPrefix}{message}");
        }

        private static void LogError(string message)
        {
            if (!GameServiceSettingsAsset.Settings.logErrors)
            {
                return;
            }
            
            Debug.LogError($"{LogPrefix}{message}");
        }
        #endregion

        private class RegisteredService
        {
            public int SortOrder;
            public Type ServiceType;
            public MethodInfo StaticInvokeMethod;
            public RegisterServiceAttribute StaticMethodAttribute;
            public WeakReference<IGameService> ServiceReference;
            public bool IsRunning { get; set; } = false;

            internal ServiceState StaticServiceState;
            
            public ServiceState CurrentState
            {
                get
                {
                    if (!IsRunning)
                    {
                        return ServiceState.Inactive;
                    }

                    if (StaticInvokeMethod != null)
                    {
                        return StaticServiceState;
                    }

                    return ServiceReference.TryGetTarget(out var service) 
                        ? service.State 
                        : ServiceState.Inactive;
                }
            }
        }
    }
}