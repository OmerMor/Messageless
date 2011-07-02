using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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
        private readonly IMessageHandler m_handler;

        protected AbstractInterceptor(ITransport transport, IKernel kernel, ISerializer serializer, IMessageHandler handler)
        {
            m_transport = transport;
            m_kernel = kernel;
            m_serializer = serializer;
            m_handler = handler;
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
            //.ForEach(storeTimeoutAction);
        }

        private string storeCallback(Delegate callback)
        {
            var token = Guid.NewGuid().ToString();
            m_kernel.Register(Component.For<Delegate>().Instance(callback).Named(token));
            Console.WriteLine("registered callback " + token);
            scheduleTimeoutAction(token, callback);
            return token;
        }

        private void scheduleTimeoutAction(string token, Delegate callback)
        {
            var ctx = MessagelessContext.CurrentContext;
            if (ctx == null)
                return;
            if (ctx.TimeOut == default(TimeSpan))
                return;
            Observable
                .Timer(ctx.TimeOut)
                .Select(_ =>
                {
                    var context = new Context {RecipientKey = token, TimeOut = ctx.TimeOut, CallbackTimedOut = true};
                    var callbackMessage = new CallbackMessage(context, callback.GetType(), null);
                    callbackMessage.Arguments = new object[callbackMessage.Method.GetParameters().Length];
                    var payload = m_serializer.Serialize(callbackMessage);
                    var transportMessage = new TransportMessage(payload, m_transport.LocalPath);
                    return transportMessage;
                })
                .Subscribe(m_transport);
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