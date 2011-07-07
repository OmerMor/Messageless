using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;

namespace Messageless
{
    public class TimeoutManager
    {
        private readonly ConcurrentDictionary<string, IDisposable> m_timeoutTimers = new ConcurrentDictionary<string, IDisposable>();
        private readonly ITransport m_transport;
        private readonly ISerializer m_serializer;

        public TimeoutManager(ITransport transport, ISerializer serializer)
        {
            m_transport = transport;
            m_serializer = serializer;
        }

        public void ScheduleTimeoutAction(string token, Delegate callback)
        {
            var ctx = MessagelessContext.CurrentContext;
            if (ctx == null)
                return;
            if (ctx.TimeOut == default(TimeSpan))
                return;
            var subscription = Observable
                .Timer(ctx.TimeOut)
                .Select(_ =>
                {
                    var context = new Context { RecipientKey = token, TimeOut = ctx.TimeOut, CallbackTimedOut = true };
                    var callbackMessage = new CallbackMessage(context, callback.GetType(), null);

                    var payload = m_serializer.Serialize(callbackMessage);
                    var transportMessage = new TransportMessage(payload, m_transport.LocalPath);
                    return transportMessage;
                })
                .Finally(() => DismissTimeoutAction(token))
                .Subscribe(m_transport);

            m_timeoutTimers[token] = subscription;
        }

        public void DismissTimeoutAction(string token)
        {
            IDisposable timeoutSubscription;
            var actionFound = m_timeoutTimers.TryRemove(token, out timeoutSubscription);
            if (!actionFound)
                return;
            timeoutSubscription.Dispose();
        }
    }
}