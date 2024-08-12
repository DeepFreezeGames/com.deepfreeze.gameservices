using System.Threading.Tasks;
using UnityEngine;

namespace GameServices.Samples.BasicServicesExample
{
    /// <summary>
    /// An example of a static service. Static services 
    /// </summary>
    public static class BasicStaticService
    {

        [RegisterService(sortOrder:10)]
        private static async Task Initialize()
        {
            //Initialize service internals here
            await Task.Delay(1000);
            
            /*Since static services aren't meant to be destroyed while the game is running, best practice is to bind to
            the application quitting listener and perform any cleanup operations when it is being shutdown. */
            Application.quitting += Cleanup;
        }

        public static void DoStaticServiceThing()
        {
            Debug.Log("Doing static service thing...");
            Debug.Log("Static service thing done");
        }

        private static void Cleanup()
        {
            //Clean up any service internals etc.
        }
    }
}