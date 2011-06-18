using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Castle.Core;
using Castle.Core.Interceptor;
using Castle.DynamicProxy;

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
            var address = m_address ?? m_target.Configuration.Attributes[WindsorEx.ADDRESS];
            var key = m_target.Configuration.Attributes[WindsorEx.REMOTE_KEY];
            Console.WriteLine("invoking {0} on {1}", invocation, address);
            var payload = serialize(invocation);
            var transportMessage = new TransportMessage(payload, address, key);
            m_transport.OnNext(transportMessage);
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