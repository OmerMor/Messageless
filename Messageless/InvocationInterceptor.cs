using System;
using Castle.Core;
using Castle.Core.Interceptor;
using Castle.DynamicProxy;
using Castle.MicroKernel;

namespace Messageless
{
    public class InvocationInterceptor : AbstractInterceptor, IInterceptor, IOnBehalfAware
    {
        private ComponentModel m_target;
        private readonly string m_address;

        public InvocationInterceptor(ITransport transport, IKernel kernel, ISerializer serializer)
            : this(null, transport, kernel, serializer)
        {
        }

        // ReSharper disable MemberCanBePrivate.Global
        public InvocationInterceptor(string address, ITransport transport, IKernel kernel, ISerializer serializer)
            : base(transport, kernel, serializer)
        // ReSharper restore MemberCanBePrivate.Global
        {
            m_address = address;
        }

        public void Intercept(IInvocation invocation)
        {
            assertIsValid(invocation.Method);

            var address = m_address ?? m_target.Configuration.Attributes[WindsorEx.ADDRESS];
            var key = m_target.Configuration.Attributes[WindsorEx.REMOTE_KEY];
            Console.WriteLine("invoking {0} on {1}", invocation, address);

            var invocationPayload = new InvocationMessage(invocation);
            replaceCallbacksWithTokens(invocationPayload);
            var payload = m_serializer.Serialize(invocationPayload);
            var transportMessage = new TransportMessage(payload, address, key);

            m_transport.OnNext(transportMessage);
        }

        public void SetInterceptedComponentModel(ComponentModel target)
        {
            m_target = target;
        }
    }
}