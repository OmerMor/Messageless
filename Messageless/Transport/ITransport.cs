using System.Reactive.Subjects;

namespace Messageless.Transport
{
    public interface ITransport : ISubject<TransportMessage>
    {
        void Init(string path);
        string LocalPath { get; }
    }
}