using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Messageless
{
    public class InProcTransport : ITransport, IDisposable
    {
        private static readonly ConcurrentDictionary<string, ISubject<TransportMessage>> s_subjects 
            = new ConcurrentDictionary<string, ISubject<TransportMessage>>();

        private readonly EventLoopScheduler m_eventLoopScheduler
            = new EventLoopScheduler(start => new Thread(start)
                                              {
                                                  IsBackground = true,
                                                  Name = "Transport." + Guid.NewGuid(),
                                              });

        public string LocalPath { get; private set; }
        public static void Start()
        {
            s_subjects.Clear();
        }
        public void OnNext(TransportMessage value)
        {
            value.SenderPath = LocalPath;
            getSubject(value.RecipientPath).OnNext(value);
        }

        public void Schedule(TransportMessage value, TimeSpan delay)
        {
            Observable
                .Return(value)
                .Delay(delay)
                .Subscribe(this);
        }

        public void OnError(Exception error)
        {
            throw error;
        }

        public void OnCompleted()
        {
            //throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<TransportMessage> observer)
        {
            var subject = getSubject(LocalPath);
            return subject
                .ObserveOn(m_eventLoopScheduler)
                .Do(msg => Console.WriteLine("transport"))
                .Subscribe(observer);
        }

        private ISubject<TransportMessage> getSubject(string path)
        {
            return s_subjects.GetOrAdd(path, _ => new ReplaySubject<TransportMessage>());
        }

        public void Init(string path)
        {
            LocalPath = path;
        }

        public void Dispose()
        {
            m_eventLoopScheduler.Dispose();
        }
    }
}