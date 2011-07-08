namespace Messageless.Transport
{
    public interface ITransportAware
    {
        void SetTransportMessage(TransportMessage transportMessage);
    }
}