using System;
using System.Reflection;

namespace Messageless
{
    [Serializable]
    public class CallbackMessage : IMessage
    {
        public Context Context { get; set; }
        public Type DelegateType { get; set; }
        public object[] Arguments { get; set; }

        public MethodInfo Method
        {
            get { return DelegateType.GetMethod("Invoke"); }
        }

        public CallbackMessage(Context context, Type delegateType, object[] arguments)
        {
            Context = context;
            DelegateType = delegateType;
            Arguments = arguments;
        }
    }
}