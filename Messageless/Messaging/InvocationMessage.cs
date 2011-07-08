using System;
using System.Reflection;
using Castle.DynamicProxy;
using Messageless.Transport;

namespace Messageless.Messaging
{
    [Serializable]
    public class InvocationMessage : IMessage, ITransportAware
    {
        public Context Context { get; set; }
        public MethodInfo Method { get; set; }
        public object[] Arguments { get; set; }

        public InvocationMessage(Context context, IInvocation invocation)
            : this(context, invocation.Method, invocation.Arguments)
        {
        }

        public InvocationMessage(Context context, MethodInfo method, object[] arguments)
        {
            Context = context;
            Method = method;
            Arguments = arguments;
        }

        public void SetTransportMessage(TransportMessage transportMessage)
        {
            Context.SenderPath = transportMessage.SenderPath;
        }

    }
}