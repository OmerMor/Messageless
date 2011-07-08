using System;
using System.Linq;
using Castle.Core;
using Castle.MicroKernel;
using Messageless.Serialization;
using Messageless.Transport;

namespace Messageless.Messaging
{
    public class MessageHandler : IStartable, IMessageHandler
    {
        private readonly IKernel m_kernel;
        private readonly ISerializer m_serializer;
        private readonly ITransport m_transport;
        private IDisposable m_subscription;
        private readonly TimeoutManager m_timeoutManager;
        private readonly object m_locker = new object();

        public MessageHandler(IKernel kernel, ITransport transport, ISerializer serializer, TimeoutManager timeoutManager)
        {
            m_kernel = kernel;
            m_transport = transport;
            m_serializer = serializer;
            m_timeoutManager = timeoutManager;
        }

        // TODO: use rx oftype
        public void Handle(TransportMessage message)
        {
            try
            {
                var msg = m_serializer.Deserialize(message.Payload);

                if (msg is ITransportAware)
                    (msg as ITransportAware).SetTransportMessage(message);

                if (msg is InvocationMessage)
                {
                    Handle((InvocationMessage)msg);
                }
                else
                {
                    Handle((CallbackMessage)msg);
                }
            }
            catch (Exception ex)
            {
                // TODO: log
                Console.WriteLine(ex);
            }
        }

        public void Handle(InvocationMessage msg)
        {
            Console.WriteLine("resolving " + msg.Context.RecipientKey);
            var target = m_kernel.Resolve(msg.Context.RecipientKey, msg.Method.DeclaringType);

            replaceTokensWithCallbackProxies(msg, msg.Context.SenderPath);
            msg.Method.Invoke(target, msg.Arguments);
        }

        public void Handle(CallbackMessage msg)
        {
            var token = msg.Context.RecipientKey;
            m_timeoutManager.DismissTimeoutAction(token);

            Delegate callback;
            lock (m_locker)
            {
                if (!m_kernel.HasComponent(token)) 
                    return;

                callback = m_kernel.Resolve<Delegate>(token);
                m_kernel.RemoveComponent(token);
            }

            Console.WriteLine("resolved and removed " + token);
            replaceTokensWithCallbackProxies(msg, msg.Context.SenderPath);
            MessagelessContext.Execute(context => callback.DynamicInvoke(msg.Arguments), msg.Context);
        }

        private void replaceTokensWithCallbackProxies(IMessage invocation, string senderPath)
        {
            var parameters = invocation.Method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var isDelegate = parameters[i].ParameterType.IsDelegate();
                if(!isDelegate)
                    continue;
                if (invocation.Arguments[i] == null)
                    continue;

                var token = (string)invocation.Arguments[i];
                var callbackProxy = tokenToCallbackProxy(token, parameters[i].ParameterType, senderPath);
                invocation.Arguments[i] = callbackProxy;
            }
        }

        private Delegate tokenToCallbackProxy(string token, Type callbackType, string senderPath)
        {
            var context = new Context{RecipientPath = senderPath, RecipientKey = token};
            var callbackInterceptor = new CallbackInterceptor(context, callbackType, m_transport, m_serializer, m_kernel, m_timeoutManager);
            var callbackMethodInfo = callbackType.GetMethod("Invoke");
            var parameterTypes = callbackMethodInfo.GetParameters()
                .Select(p => p.ParameterType)
                .ToArray();
            var method = callbackInterceptor.GetType().GetMethods()
                .Where(mi => mi.Name == "Intercept")
                .Single(mi =>mi.GetParameters().Count() == parameterTypes.Length);
            if (method.IsGenericMethodDefinition)
                method = method.MakeGenericMethod(parameterTypes);
            var callbackProxy = Delegate.CreateDelegate(callbackType, callbackInterceptor,
                                                        method, throwOnBindFailure: true);
            return callbackProxy;
        }

        #region Implementation of IStartable

        public void Start()
        {
            m_subscription = m_transport.Subscribe(Handle, Console.WriteLine, 
                () => Console.WriteLine("OnCompleted"));
        }

        public void Stop()
        {
            m_subscription.Dispose();
        }

        #endregion
    }
}