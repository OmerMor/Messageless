using System;
using Rhino.Mocks.Interfaces;

namespace Messageless.Tests
{
    public static class RhinoMocksEx
    {
        public class CallOptions<T>
        {
            private readonly IMethodOptions<T> m_methodOptions;

            public CallOptions(IMethodOptions<T> methodOptions)
            {
                m_methodOptions = methodOptions;
            }

            public IMethodOptions<T> Action(Action action)
            {
                return m_methodOptions.Do(action);
            }
            public IMethodOptions<T> Action<T1>(Action<T1> action)
            {
                return m_methodOptions.Do(action);
            }
            public IMethodOptions<T> Action<T1, T2>(Action<T1, T2> action)
            {
                return m_methodOptions.Do(action);
            }
            public IMethodOptions<T> Action<T1, T2,T3>(Action<T1, T2,T3> action)
            {
                return m_methodOptions.Do(action);
            }
            public IMethodOptions<T> Func<R>(Func<R> action)
            {
                return m_methodOptions.Do(action);
            }
        }
        public static CallOptions<T> Call<T>(this IMethodOptions<T> methodOptions)
        {
            return new CallOptions<T>(methodOptions);
        }
    }
}