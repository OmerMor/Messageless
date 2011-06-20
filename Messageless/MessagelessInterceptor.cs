using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Castle.Core;
using Castle.Core.Interceptor;
using Castle.DynamicProxy;
using System.Linq;

namespace Messageless
{
    public class MessagelessInterceptor : IInterceptor, IOnBehalfAware
    {
        private ComponentModel m_target;
        private readonly string m_address;
        private readonly ITransport m_transport;
        private readonly IFormatter m_formatter;

        public MessagelessInterceptor(ITransport transport)
            : this(null, transport)
        {
        }

        // ReSharper disable MemberCanBePrivate.Global
        public MessagelessInterceptor(string address, ITransport transport)
        // ReSharper restore MemberCanBePrivate.Global
        {
            m_address = address;
            m_transport = transport;
            m_formatter = new BinaryFormatter();
        }

        public void Intercept(IInvocation invocation)
        {
            assertIsValid(invocation);

            var address = m_address ?? m_target.Configuration.Attributes[WindsorEx.ADDRESS];
            var key = m_target.Configuration.Attributes[WindsorEx.REMOTE_KEY];
            Console.WriteLine("invoking {0} on {1}", invocation, address);
            var payload = serialize(invocation);
            var transportMessage = new TransportMessage(payload, address, key);

            m_transport.OnNext(transportMessage);
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