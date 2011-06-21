using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Castle.Core;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using System;
using System.Linq;

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
                var msg = formatter.Deserialize(stream);
                if (msg is MessageInvocation)
                {
                    var invocation = (MessageInvocation)msg;

                    var target = m_kernel.Resolve(message.Key, invocation.Method.DeclaringType);

                    replaceTokensWithCallbackProxies(invocation, message.SenderPath);

                    invocation.InvocationTarget = target;
                    invocation.Proceed();
                }
                var cbMsg = (CallbackMessage)msg;

                var callback = m_kernel.Resolve<Delegate>(message.Key);
                m_kernel.RemoveComponent(message.Key);
                callback.DynamicInvoke(cbMsg.Args);
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

        private void replaceTokensWithCallbackProxies(IInvocation invocation, string senderPath)
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
            var callbackInterceptor = new CallbackInterceptor(m_transport, context);
            var callbackMethodInfo = callbackType.GetMethod("Invoke");
            var parameterTypes = callbackMethodInfo.GetParameters()
                .Select(p => p.ParameterType).ToArray();
            var openGenericMethod = callbackInterceptor.GetType().GetMethods()
                .Where(mi => mi.Name == "Intercept")
                .Single(mi =>mi.GetParameters().Count() == parameterTypes.Length);
            var closeGenericMethod = openGenericMethod.MakeGenericMethod(parameterTypes);
            var callbackProxy = Delegate.CreateDelegate(callbackType, callbackInterceptor,
                                                        closeGenericMethod, true);
            return callbackProxy;
        }

        [Serializable]
        public class Context
        {
            public Guid Token { get; set; }
            public string Path { get; set; }

            public Context(Guid token, string path)
            {
                Token = token;
                Path = path;
            }
        }
        [Serializable]
        public class CallbackMessage
        {
            public Context Context { get; set; }
            public object[] Args { get; set; }

            public CallbackMessage(Context context, object[] args)
            {
                Context = context;
                Args = args;
            }
        }
        public class CallbackInterceptor
        {
            private readonly Context m_context;
            private readonly ITransport m_transport;

            public CallbackInterceptor(ITransport transport, Context context)
            {
                m_transport = transport;
                m_context = context;
            }

            public void Intercept<T>(T arg1)
            {
                Console.WriteLine("CallbackInterceptor.Intercept");
                var msg = new CallbackMessage(m_context, new object[] {arg1});
                var serializer = new BinaryFormatter();
                var stream = new MemoryStream();
                serializer.Serialize(stream, msg);
                var payload = stream.ToArray();
                m_transport.OnNext(new TransportMessage(payload, m_context.Path, m_context.Token.ToString()));
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

    internal static class Utils
    {
        public static bool IsDelegate(this Type type)
        {
            return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
        }
    }
}