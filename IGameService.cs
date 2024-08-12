using System.Threading.Tasks;

namespace GameServices
{
    /// <summary>
    /// The inherited interface for all instanced game services.
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// The order used to sort the services. This prevents race conditions when initializing. Best practices is to
        /// increment services by multiples of 10 or 100 to allow for easy expansion as your project grows.
        /// </summary>
        int SortOrder { get; }
        
        /// <summary>
        /// Should return the current state of the service. This field should be initialized as <see cref="ServiceState.Inactive"/>
        /// when declaring the inherited class. Best practice is to declare the setter as private or protected.
        /// </summary>
        ServiceState State { get; }

        /// <summary>
        /// The method that is populated with the initialization logic for the service. 
        /// </summary>
        Task Initialize();

        /// <summary>
        /// 
        /// </summary>
        void Shutdown();
    }
}