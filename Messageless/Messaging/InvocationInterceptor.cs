using System;
using Castle.Core;
using Castle.Core.Interceptor;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using Messageless.IoC;
using Messageless.Serialization;
using Messageless.Transport;

namespace Messageless.Messaging
{
    public class InvocationInterceptor : AbstractInterceptor, IInterceptor, IOnBehalfAware
    {
        private ComponentModel m_target;
        private readonly string m_address;

        // ReSharper disable MemberCanBePrivate.Global
        public InvocationInterceptor(string address, ITransport transport, IKernel kernel, ISerializer serializer, TimeoutManager timeoutManager)
            : base(transport, kernel, serializer, timeoutManager)
        // ReSharper restore MemberCanBePrivate.Global
        {
            m_address = address;
        }

        public InvocationInterceptor(ITransport transport, IKernel kernel, ISerializer serializer, TimeoutManager timeoutManager)
            : this(null, transport, kernel, serializer, timeoutManager)
        {
        }

        public void Intercept(IInvocation invocation)
        {
            assertIsValid(invocation.Method);
            // TODO: fix dependency on IoC
            var address = m_address ?? m_target.Configuration.Attributes[WindsorEx.ADDRESS];
            var key = m_target.Configuration.Attributes[WindsorEx.REMOTE_KEY];
            Console.WriteLine("invoking {0} on {1}", invocation, address);
            var ctx = new Context {RecipientKey = key, RecipientPath = address};
            var invocationPayload = new InvocationMessage(ctx, invocation);
            replaceCallbacksWithTokens(invocationPayload);
            var payload = m_serializer.Serialize(invocationPayload);
            var transportMessage = new TransportMessage(payload, address);

            m_transport.OnNext(transportMessage);
        }

        public void SetInterceptedComponentModel(ComponentModel target)
        {
            m_target = target;
        }
    }
}