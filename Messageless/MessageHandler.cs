using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Castle.Core;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using System;

namespace Messageless
{
    public interface IMessageHandler
    {
    }

    public class MessageHandler : IStartable, IMessageHandler
    {
        private readonly IKernel m_kernel;
        private readonly ITransport m_transport;
        private IDisposable m_subscription;

        public MessageHandler(IKernel kernel, ITransport transport)
        {
            m_kernel = kernel;
            m_transport = transport;
        }

        private void handleMessage(TransportMessage message)
        {
            try
            {
                var formatter = new BinaryFormatter();
                var stream = new MemoryStream(message.Payload);

                var invocation = (MessageInvocation)formatter.Deserialize(stream);

                var target = m_kernel.Resolve(message.Key, invocation.Method.DeclaringType);
                invocation.InvocationTarget = target;
                invocation.Proceed();
            }
            catch (Exception ex)
            {
                //TODO: log
                Console.WriteLine(ex);
            }
        }

        #region Implementation of IStartable

        public void Start()
        {
            m_subscription = m_transport.Subscribe(handleMessage);
        }

        public void Stop()
        {
            m_subscription.Dispose();
        }

        #endregion
    }
}