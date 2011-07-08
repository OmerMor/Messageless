using Messageless.Transport;

namespace Messageless.Messaging
{
    public interface IMessageHandler
    {
        void Handle(TransportMessage msg);
        void Handle(InvocationMessage msg);
        void Handle(CallbackMessage msg);
    }
}