using System.Threading.Tasks;
using UnityEngine.Scripting;

namespace GameServices.Samples.BasicServicesExample
{
    /// <summary>
    /// An example of an instanced service.
    /// </summary>
    /// 
    //Since the service manager uses reflection to find services, best practice is to add a [Preserve] attribute to prevent the service from being stripped
    [Preserve]
    public sealed class InstancedService : IGameService
    {
        public int SortOrder => 0;

        //Best practices is to declare the setter as private or protected
        public ServiceState State { get; private set; }
        
        public async Task Initialize()
        {
            //Always check to make sure the service isn't already running
            if (State != ServiceState.Inactive && State != ServiceState.Error)
            {
                throw new ServiceAlreadyInitializedException(typeof(InstancedService));
            }

            State = ServiceState.Starting;
            
            //Initialize service internals here
            await Task.Delay(1000);

            State = ServiceState.Running;
        }

        public void Shutdown()
        {
            State = ServiceState.Stopping;

            //Clean up service internals
            
            /*
             * Best practice is to always allow the service to shut down and perform validation logic on the items that
             * are cleaned up so that the service can be reliably shutdown even in the event of a failure.
             */

            State = ServiceState.Inactive;
        }
    }
}