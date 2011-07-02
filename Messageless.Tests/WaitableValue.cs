using System;
using System.Threading.Tasks;

namespace Messageless.Tests
{
    public class WaitableValue<T>
    {
        private readonly TaskCompletionSource<T> m_source = new TaskCompletionSource<T>();

        public T Value
        {
            get { return m_source.Task.Result; }
            set { m_source.SetResult(value); }
        }

        public bool WaitOne(TimeSpan timeout)
        {
            return m_source.Task.Wait(timeout);
        }
    }
}