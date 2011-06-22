using System;

namespace Messageless
{
    public class CallbackInterceptor
    {
        private readonly Context m_context;
        private readonly Type m_delegateType;
        private readonly ITransport m_transport;
        private readonly ISerializer m_serializer;

        public CallbackInterceptor(Context context, Type delegateType, ITransport transport, ISerializer serializer)
        {
            m_transport = transport;
            m_context = context;
            m_serializer = serializer;
            m_delegateType = delegateType;
        }

        public void Intercept()
        {
            intercept();
        }

        public void Intercept<T1>(T1 a1)
        {
            intercept(a1);
        }

        public void Intercept<T1, T2>(T1 a1, T2 a2)
        {
            intercept(a1, a2);
        }

        public void Intercept<T1, T2, T3>(T1 a1, T2 a2, T3 a3)
        {
            intercept(a1, a2, a3);
        }

        public void Intercept<T1, T2, T3, T4>(T1 a1, T2 a2, T3 a3, T4 a4)
        {
            intercept(a1, a2, a3, a4);
        }

        public void Intercept<T1, T2, T3, T4, T5>(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5)
        {
            intercept(a1, a2, a3, a4, a5);
        }

        public void Intercept<T1, T2, T3, T4, T5, T6>
            (T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6)
        {
            intercept(a1, a2, a3, a4, a5, a6);
        }

        public void Intercept<T1, T2, T3, T4, T5, T6, T7>
            (T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7)
        {
            intercept(a1, a2, a3, a4, a5, a6, a7);
        }

        public void Intercept<T1, T2, T3, T4, T5, T6, T7, T8>
            (T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7, T8 a8)
        {
            intercept(a1, a2, a3, a4, a5, a6, a7, a8);
        }

        private void intercept(params object[] args)
        {
            var msg = new CallbackMessage(m_context, m_delegateType, args);
            var payload = m_serializer.Serialize(msg);
            var transportMessage = new TransportMessage(payload, m_context.Path, m_context.Token.ToString());
            m_transport.OnNext(transportMessage);
        }
    }
}