using System;
using System.Reactive.Concurrency;
using System.Threading;

namespace Messageless.Transport
{
    public abstract class AbstractTransport : ITransport, IDisposable
    {
        protected EventLoopScheduler m_eventLoopScheduler;
        public string LocalPath { get; private set; }

        private string Name
        {
            get { return string.Format("transport:{0}", LocalPath); }
        }

        public abstract void OnNext(TransportMessage value);

        public void OnError(Exception error)
        {
            throw error;
        }

        public void OnCompleted()
        {
            // ignore
        }

        public virtual void Init(string path)
        {
            LocalPath = path;
            Console.WriteLine("starting Transport " + Name);
            m_eventLoopScheduler = new EventLoopScheduler(start => new Thread(start)
                                                                   {
                                                                       IsBackground = true,
                                                                       Name = Name,
                                                                   });
        }

        public virtual void Dispose()
        {
            if (m_eventLoopScheduler != null) 
                m_eventLoopScheduler.Dispose();
            m_eventLoopScheduler = null;
            Console.WriteLine("disposed Transport " + Name);
        }

        public abstract IDisposable Subscribe(IObserver<TransportMessage> observer);
    }
}