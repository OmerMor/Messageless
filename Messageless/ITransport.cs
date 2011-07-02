using System;
using System.Reactive.Subjects;

namespace Messageless
{
    public interface ITransport : ISubject<TransportMessage>
    {
        //IObservable<TransportMessage> MessageReceived { get; }
        void Init(string path);
        void Schedule(TransportMessage value, TimeSpan delay);
        string LocalPath { get; }
    }
}