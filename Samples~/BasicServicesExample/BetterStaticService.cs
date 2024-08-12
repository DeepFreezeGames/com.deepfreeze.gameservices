using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace GameServices.Samples.BasicServicesExample
{
    /// <summary>
    /// The recommended way of building services. Provides the best of both worlds between instanced and static services.
    /// This approach can be overkill for simpler services. For those scenarios, the <see cref="BasicStaticService"/>
    /// approach may be more appropriate.
    /// </summary>
    [Preserve]
    public static class BetterStaticService
    {
        /// <summary>
        /// The underlying, managed instance of the service. This is what does all the heavy lifting and the static class
        /// is simply a wrapper for handling logic.
        /// </summary>
        private static BetterStaticServiceInstance Instance { get; set; }
        
        [RegisterService(sortOrder:20)]
        private static async Task Initialize()
        {
            if (Instance != null)
            {
                Debug.LogError($"Trying to initialize {nameof(BetterStaticService)} but an instance is already running");
                return;
            }
            
            try
            {
                Instance = new BetterStaticServiceInstance();
                await Instance.Initialize();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to initialize {nameof(BetterStaticService)}\nException: {exception}");
                throw; //Exceptions should be thrown so that the ServiceLoader knows about them
            }
        }

        public static void DoServiceThing()
        {
            if (Instance is not { Initialized: true })
            {
                Debug.LogError($"Trying to {nameof(DoServiceThing)} but the service instance is not yet initialized");
                return;
            }
            
            Instance.DoServiceThing();
        }
        
        public static async Task DoServiceThingAsync()
        {
            if (Instance is not { Initialized: true })
            {
                Debug.LogError($"Trying to {nameof(DoServiceThingAsync)} but the service instance is not yet initialized");
                return;
            }

            await Instance.DoServiceThingAsync();
        }
    }

    /// <summary>
    /// The managed service instance should be an internal class since the only thing that should talk to it is the outer
    /// wrapper class (e.g. <see cref="BetterStaticService"/>)
    /// </summary>
    internal class BetterStaticServiceInstance
    {
        public bool Initialized { get; private set; }
        
        internal async Task Initialize()
        {
            if (Initialized)
            {
                Debug.LogError($"Trying to initialize {nameof(BetterStaticServiceInstance)} but it is already initialized");
                return;
            }
            
            await Task.Delay(1000);
            Initialized = true;
        }

        public void DoServiceThing()
        {
            
        }

        public async Task DoServiceThingAsync()
        {
            await Task.Delay(100);
        }
    }
}