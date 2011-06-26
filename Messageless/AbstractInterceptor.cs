using System;
using System.Linq;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;

namespace Messageless
{
    public abstract class AbstractInterceptor
    {
        protected readonly ITransport m_transport;
        private readonly IKernel m_kernel;
        protected readonly ISerializer m_serializer;

        protected AbstractInterceptor(ITransport transport, IKernel kernel, ISerializer serializer)
        {
            m_transport = transport;
            m_kernel = kernel;
            m_serializer = serializer;
        }

        protected void replaceCallbacksWithTokens(IMessage msg)
        {
            for (var i = 0; i < msg.Arguments.Length; i++)
            {
                var callback = msg.Arguments[i] as Delegate;
                if (callback == null) continue;

                var token = storeCallback(callback);
                msg.Arguments[i] = token;
            }
        }

        private Guid storeCallback(Delegate callback)
        {
            var token = Guid.NewGuid();
            m_kernel.Register(Component.For<Delegate>().Instance(callback).Named(token.ToString()));

            return token;
        }

        protected void assertIsValid(IInvocation invocation)
        {
            var hasReturnValue = invocation.Method.ReturnType != typeof (void);
            if (hasReturnValue)
                throw new InvalidOperationException("Tried to call a method that returns a value on a proxy. ");

            var hasOutParams = invocation.Method.GetParameters().Any(pi => pi.IsOut);
            if (hasOutParams)
                throw new InvalidOperationException("Tried to call a method with out-parameters on a proxy. ");
        }
    }
}