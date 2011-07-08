using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Messageless.Messaging;

namespace Messageless.IoC
{
    public static class WindsorEx
    {
        internal const string ADDRESS = "address";
        internal const string REMOTE_KEY = "remote-key";

        public static ComponentRegistration<T> At<T>(this ComponentRegistration<T> componentRegistration, string address, string key)
        {
            return componentRegistration.At(address, key, key);
        }

        public static ComponentRegistration<T> At<T>(this ComponentRegistration<T> componentRegistration, string address, string remoteKey, string localKey)
        {
            componentRegistration = componentRegistration
                .Interceptors<InvocationInterceptor>()
                .AddAttributeDescriptor(ADDRESS, address)
                .AddAttributeDescriptor(REMOTE_KEY, remoteKey).Named(localKey);

            return componentRegistration;
        }

        public static T ResolveRemoteService<T>(this IWindsorContainer container, string address)
        {
            var arguments = new Dictionary<string, string> { { ADDRESS, address } };
            var service = container.Resolve<T>(arguments);
            return service;
        }

        public static IWindsorContainer IntegrateMessageless(this IWindsorContainer container, string path)
        {
            container.AddFacility<MessagelessFacility>(facility => facility.Init(path));
            return container;
        }
    }
}