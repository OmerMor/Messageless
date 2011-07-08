using System;
using System.Linq;
using System.Reflection;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Messageless.Serialization;
using Messageless.Transport;

namespace Messageless.Messaging
{
    public abstract class AbstractInterceptor
    {
        protected readonly ITransport m_transport;
        private readonly IKernel m_kernel;
        protected readonly ISerializer m_serializer;
        private readonly TimeoutManager m_timeoutManager;

        protected AbstractInterceptor(ITransport transport, IKernel kernel, ISerializer serializer, TimeoutManager timeoutManager)
        {
            m_transport = transport;
            m_kernel = kernel;
            m_serializer = serializer;
            m_timeoutManager = timeoutManager;
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

        private string storeCallback(Delegate callback)
        {
            var token = Guid.NewGuid().ToString();
            m_kernel.Register(Component.For<Delegate>().Instance(callback).Named(token));
            Console.WriteLine("registered callback " + token);
            m_timeoutManager.ScheduleTimeoutAction(token, callback);
            return token;
        }


        protected static void assertIsValid(MethodInfo method)
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