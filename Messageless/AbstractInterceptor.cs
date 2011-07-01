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
            msg.Arguments
                .OfType<Delegate>()
                .Select(callback => callback.Method)
                .ForEach(assertIsValid);

            msg.Arguments
                .Select((argument, index) => new {callback = argument as Delegate, index})
                .Where(t => t.callback != null)
                .ForEach(t => msg.Arguments[t.index] = storeCallback(t.callback));
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