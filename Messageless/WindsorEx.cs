using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace Messageless
{
    public static class WindsorEx
    {
        internal const string ADDRESS = "address";
        internal const string KEY = "key";
        public static ComponentRegistration<T> At<T>(this ComponentRegistration<T> cr, string address, string key)
        {
            return cr
                .Interceptors<MessagelessInterceptor>()
                .AddAttributeDescriptor(ADDRESS, address)
                .AddAttributeDescriptor(KEY, key);
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