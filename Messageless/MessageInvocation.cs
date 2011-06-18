using System;
using System.Diagnostics;
using System.Reflection;
using Castle.DynamicProxy;

namespace Messageless
{
    [Serializable]
    public class MessageInvocation : IInvocation
    {
        private object m_invocationTarget;
        private Type m_targetType;
        private object[] m_arguments;
        private Type[] m_genericArguments;
        private MethodInfo m_method;

        public override string ToString()
        {
            return string.Format("InvocationTarget: {0}, TargetType: {1}, Arguments: {2}, GenericArguments: {3}, Method: {4}, Callstack: {5}", m_invocationTarget, m_targetType, m_arguments, m_genericArguments, m_method, new StackTrace());
        }

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