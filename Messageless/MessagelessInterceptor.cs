using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Castle.Core;
using Castle.Core.Interceptor;
using Castle.DynamicProxy;
using System.Linq;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;

namespace Messageless
{
    //public class CallbackRegistry
    public class MessagelessInterceptor : IInterceptor, IOnBehalfAware
    {
        private ComponentModel m_target;
        private readonly string m_address;
        private readonly ITransport m_transport;
        private readonly IKernel m_kernel;
        private readonly IFormatter m_formatter;
        private readonly ConcurrentDictionary<Guid, MulticastDelegate> m_callbackStore = new ConcurrentDictionary<Guid, MulticastDelegate>();

        public MessagelessInterceptor(ITransport transport, IKernel kernel)
            : this(null, transport, kernel)
        {
        }

        // ReSharper disable MemberCanBePrivate.Global

        public MessagelessInterceptor(string address, ITransport transport, IKernel kernel)
        // ReSharper restore MemberCanBePrivate.Global
        {
            m_address = address;
            m_transport = transport;
            m_kernel = kernel;
            m_formatter = new BinaryFormatter();
        }

        public void Intercept(IInvocation invocation)
        {
            assertIsValid(invocation);

            var address = m_address ?? m_target.Configuration.Attributes[WindsorEx.ADDRESS];
            var key = m_target.Configuration.Attributes[WindsorEx.REMOTE_KEY];
            Console.WriteLine("invoking {0} on {1}", invocation, address);

            replaceCallbacksWithTokens(invocation);

            var payload = serialize(invocation);
            var transportMessage = new TransportMessage(payload, address, key);

            m_transport.OnNext(transportMessage);
        }

        private void replaceCallbacksWithTokens(IInvocation invocation)
        {
            for (var i = 0; i < invocation.Arguments.Length; i++)
            {
                var callback = invocation.Arguments[i] as MulticastDelegate;
                if (callback == null) continue;

                var token = storeCallback(callback);
                invocation.Arguments[i] = token;
            }
        }

        private Guid storeCallback(MulticastDelegate callback)
        {
            var token = Guid.NewGuid();
            //m_callbackStore[token] = callback;
            m_kernel.Register(Component.For<Delegate>().Instance(callback).Named(token.ToString()));

            return token;
        }

        private void assertIsValid(IInvocation invocation)
        {
            var hasReturnValue = invocation.Method.ReturnType != typeof(void);
            if (hasReturnValue)
                throw new InvalidOperationException("Tried to call a method that returns a value on a proxy. ");

            var hasOutParams = invocation.Method.GetParameters().Any(pi => pi.IsOut);
            if (hasOutParams)
                throw new InvalidOperationException("Tried to call a method with out-parameters on a proxy. ");

        }

        private byte[] serialize(IInvocation invocation)
        {
            var invocationPayload = new MessageInvocation(invocation);
            var stream = new MemoryStream();
            m_formatter.Serialize(stream, invocationPayload);
            return stream.ToArray();
        }

        public void SetInterceptedComponentModel(ComponentModel target)
        {
            m_target = target;
        }
    }
}