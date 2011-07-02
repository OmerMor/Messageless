using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Messageless
{
    public class InProcTransport : ITransport
    {
        private static readonly ConcurrentDictionary<string, ISubject<TransportMessage>> s_subjects 
            = new ConcurrentDictionary<string, ISubject<TransportMessage>>();

        public string LocalPath { get; private set; }

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
            return subject.Subscribe(observer);
        }

        private ISubject<TransportMessage> getSubject(string path)
        {
            return s_subjects.GetOrAdd(path, _ => new ReplaySubject<TransportMessage>());
        }

        public void Init(string path)
        {
            LocalPath = path;
        }
    }
}