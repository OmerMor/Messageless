using System;
using System.IO;
using System.Reflection;
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
            var key = m_target.Configuration.Attributes[WindsorEx.KEY];
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

    [Serializable]
    public class MessageInvocation : IInvocation
    {
        private object m_invocationTarget;
        private Type m_targetType;
        private object[] m_arguments;
        private Type[] m_genericArguments;
        private MethodInfo m_method;

        public MessageInvocation(IInvocation invocation)
        {
            m_invocationTarget = invocation.InvocationTarget;
            m_targetType = invocation.TargetType;
            m_arguments = invocation.Arguments;
            m_genericArguments = invocation.GenericArguments;
            m_method = invocation.Method;
        }

        #region Implementation of IInvocation

        public void SetArgumentValue(int index, object value)
        {
            m_arguments[index] = value;
        }

        public object GetArgumentValue(int index)
        {
            return m_arguments[index];
        }

        public MethodInfo GetConcreteMethod()
        {
            throw new NotImplementedException();
        }

        public MethodInfo GetConcreteMethodInvocationTarget()
        {
            throw new NotImplementedException();
        }

        public void Proceed()
        {
            Method.Invoke(InvocationTarget, Arguments);
        }

        public object Proxy
        {
            get { return null; }
        }

        public object InvocationTarget
        {
            get { return m_invocationTarget; }
            set { m_invocationTarget = value; }
        }

        public Type TargetType
        {
            get { return m_targetType; }
        }

        public object[] Arguments
        {
            get { return m_arguments; }
        }

        public Type[] GenericArguments
        {
            get { return m_genericArguments; }
        }

        public MethodInfo Method
        {
            get { return m_method; }
        }

        public MethodInfo MethodInvocationTarget
        {
            get { return null; }
        }

        public object ReturnValue
        {
            get { return null; }
            set { }
        }

        #endregion
    }
}