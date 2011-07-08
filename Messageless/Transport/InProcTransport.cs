using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Messageless.Transport
{
    public class InProcTransport : AbstractTransport
    {
        private static readonly ConcurrentDictionary<string, ReplaySubject<TransportMessage>> s_subjects
            = new ConcurrentDictionary<string, ReplaySubject<TransportMessage>>();

        public override void OnNext(TransportMessage value)
        {
            value.SenderPath = LocalPath;
            getSubject(value.RecipientPath).OnNext(value);
        }

        public override IDisposable Subscribe(IObserver<TransportMessage> observer)
        {
            var subject = getSubject(LocalPath);
            return subject
                .ObserveOn(m_eventLoopScheduler)
                .Do(msg => Console.WriteLine("transport"))
                .Subscribe(observer);
        }

        private ReplaySubject<TransportMessage> getSubject(string path)
        {
            return s_subjects.GetOrAdd(path, _ => new ReplaySubject<TransportMessage>());
        }

        public override void Dispose()
        {
            ReplaySubject<TransportMessage> subject;
            var found = s_subjects.TryRemove(LocalPath, out subject);
            if (found)
            {
                subject.OnCompleted();
                subject.Dispose();
            }
            base.Dispose();
        }

    }
}