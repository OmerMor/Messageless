using System;
using System.Reflection;
using Castle.DynamicProxy;

namespace Messageless
{
    [Serializable]
    public class CallbackMessage
    {
        public Context Context { get; set; }
        public object[] Arguments { get; set; }

        public CallbackMessage(Context context, object[] arguments)
        {
            Context = context;
            Arguments = arguments;
        }
    }

    [Serializable]
    public class InvocationMessage
    {
        public MethodInfo Method { get; set; }
        public object[] Arguments { get; set; }
        public object Target { get; set; }

        public InvocationMessage(IInvocation invocation)
            : this(invocation.InvocationTarget, invocation.Method, invocation.Arguments)
        {
        }

        public InvocationMessage(object target, MethodInfo method, object[] arguments)
        {
            Target = target;
            Method = method;
            Arguments = arguments;
        }
    }
}