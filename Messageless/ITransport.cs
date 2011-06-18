using System.Reactive.Subjects;

namespace Messageless
{
    public interface ITransport : ISubject<TransportMessage>
    {
        //IObservable<TransportMessage> MessageReceived { get; }
        void Init(string path);
    }
}