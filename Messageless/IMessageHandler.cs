namespace Messageless
{
    public interface IMessageHandler
    {
        void Handle(TransportMessage msg);
        void Handle(InvocationMessage msg);
        void Handle(CallbackMessage msg);
    }
}