using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Castle.Core;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using System;
using System.Linq;

namespace Messageless
{
    public class MessageHandler : IStartable, IMessageHandler
    {
        private readonly IKernel m_kernel;
        private readonly ISerializer m_serializer;
        private readonly ITransport m_transport;
        private IDisposable m_subscription;

        public MessageHandler(IKernel kernel, ITransport transport, ISerializer serializer)
        {
            m_kernel = kernel;
            m_transport = transport;
            m_serializer = serializer;
        }

        private void handleMessage(TransportMessage message)
        {
            try
            {
                var msg = m_serializer.Deserialize(message.Payload);
                if (msg is InvocationMessage)
                {
                    var invocationMessage = (InvocationMessage)msg;

                    var target = m_kernel.Resolve(message.Key, invocationMessage.Method.DeclaringType);

                    replaceTokensWithCallbackProxies(invocationMessage, message.SenderPath);
                    invocationMessage.Method.Invoke(target, invocationMessage.Arguments);
                    return;
                }
                var cbMsg = (CallbackMessage)msg;

                var callback = m_kernel.Resolve<Delegate>(message.Key);
                m_kernel.RemoveComponent(message.Key);
                callback.DynamicInvoke(cbMsg.Arguments);
                //replaceTokensWithCallbackProxies(invocation, message.SenderPath);

                //invocation.InvocationTarget = target;
                //invocation.Proceed();
 
            }
            catch (Exception ex)
            {
                //TODO: log
                Console.WriteLine(ex);
            }
        }

        private void replaceTokensWithCallbackProxies(InvocationMessage invocation, string senderPath)
        {
            var parameters = invocation.Method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var isDelegate = parameters[i].ParameterType.IsDelegate();
                if(!isDelegate)
                    continue;
                if (invocation.Arguments[i] == null)
                    continue;

                var token = (Guid)invocation.Arguments[i];
                var callbackProxy = tokenToCallbackProxy(token, parameters[i].ParameterType, senderPath);
                invocation.Arguments[i] = callbackProxy;
            }
        }

        private Delegate tokenToCallbackProxy(Guid token, Type callbackType, string senderPath)
        {
            var context = new Context(token, senderPath);
            var callbackInterceptor = new CallbackInterceptor(context, m_transport, m_serializer);
            var callbackMethodInfo = callbackType.GetMethod("Invoke");
            var parameterTypes = callbackMethodInfo.GetParameters()
                .Select(p => p.ParameterType)
                .ToArray();
            var openGenericMethod = callbackInterceptor.GetType().GetMethods()
                .Where(mi => mi.Name == "Intercept")
                .Single(mi =>mi.GetParameters().Count() == parameterTypes.Length);
            var closeGenericMethod = openGenericMethod.MakeGenericMethod(parameterTypes);
            var callbackProxy = Delegate.CreateDelegate(callbackType, callbackInterceptor,
                                                        closeGenericMethod, throwOnBindFailure: true);
            return callbackProxy;
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