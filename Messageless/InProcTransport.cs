using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace Messageless
{
    public class InProcTransport : ITransport
    {
        private static readonly ConcurrentDictionary<string, ISubject<TransportMessage>> s_subjects = new ConcurrentDictionary<string, ISubject<TransportMessage>>();
        private string m_localPath;

        public void OnNext(TransportMessage value)
        {
            value.SenderPath = m_localPath;
            getSubject(value.Path).OnNext(value);
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<TransportMessage> observer)
        {
            var subject = getSubject(m_localPath);
            return subject.Subscribe(observer);
        }

        private ISubject<TransportMessage> getSubject(string path)
        {
            return s_subjects.GetOrAdd(path, _ => new ReplaySubject<TransportMessage>());
        }

        public void Init(string path)
        {
            m_localPath = path;
        }
    }
}