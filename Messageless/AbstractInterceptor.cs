using System;
using System.Linq;
using System.Reflection;
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
            foreach (var callback in msg.Arguments.OfType<Delegate>())
            {
                assertIsValid(callback.Method);
            }

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

        protected void assertIsValid(MethodInfo method)
        {
            var hasReturnValue = method.ReturnType != typeof (void);
            if (hasReturnValue)
                throw new InvalidOperationException("Methods or delegates with return value are not supported.");

            var hasOutParams = method.GetParameters().Any(parameter => parameter.IsOut);
            if (hasOutParams)
                throw new InvalidOperationException("Methods or delegates with out parameters are not supported. ");
        }
    }
}