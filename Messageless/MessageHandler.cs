using System.IO;
using System.Reflection;
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

                replaceTokensWithCallbackProxies(invocation);

                invocation.InvocationTarget = target;
                invocation.Proceed();
            }
            catch (Exception ex)
            {
                //TODO: log
                Console.WriteLine(ex);
            }
        }

        private void replaceTokensWithCallbackProxies(IInvocation invocation)
        {
            var parameters = invocation.Method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var isDelegate = parameters[i].ParameterType.IsSubclassOf(typeof(MulticastDelegate));
                if(!isDelegate)
                    continue;

                invocation.Arguments[i] = null;
            }
        }

        #region Implementation of IStartable

        public void Start()
        {
            m_subscription = m_transport.Subscribe(handleMessage, Console.WriteLine, () => Console.WriteLine("OnCompleted"));
        }

        public void Stop()
        {
            m_subscription.Dispose();
        }

        #endregion
    }
}