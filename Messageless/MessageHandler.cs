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
                //TODO: log
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
            Console.WriteLine("resolving " + msg.Context.RecipientKey);
            var callback = m_kernel.Resolve<Delegate>(msg.Context.RecipientKey);

            replaceTokensWithCallbackProxies(msg, msg.Context.SenderPath);
            m_kernel.RemoveComponent(msg.Context.RecipientKey);
            Console.WriteLine("removed " + msg.Context.RecipientKey);
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
            var callbackInterceptor = new CallbackInterceptor(context, callbackType, m_transport, m_serializer, m_kernel, this);
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
            m_subscription = m_transport.Subscribe(Handle, Console.WriteLine, () => Console.WriteLine("OnCompleted"));
        }

        public void Stop()
        {
            m_subscription.Dispose();
        }

        #endregion
    }

    public interface ITransportAware
    {
        void SetTransportMessage(TransportMessage transportMessage);
    }
}