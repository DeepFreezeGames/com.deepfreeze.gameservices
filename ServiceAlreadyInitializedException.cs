using System;

namespace GameServices
{
    public class ServiceAlreadyInitializedException : Exception
    {
        private Type ServiceType { get; }

        public ServiceAlreadyInitializedException(Type serviceType)
        {
            ServiceType = serviceType;
        }

        public override string Message => $"Trying to initialize a service of type {ServiceType.Name} but an instance of that service is already running";

        public override string ToString()
        {
            return Message;
        }
    }
}