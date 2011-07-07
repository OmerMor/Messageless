using Castle.MicroKernel;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Castle.Facilities.Startable;

namespace Messageless
{
    public interface IMessagelessFacility : IFacility
    {
        void Init(string path);
    }

    public class MessagelessFacility : AbstractFacility, IMessagelessFacility
    {
        private string m_path;

        protected override void Init()
        {
            Kernel.AddFacility<StartableFacility>();
            Kernel.Register(
                Component.For<IMessageHandler>().ImplementedBy<MessageHandler>().Start(),
                Component.For<InvocationInterceptor>().LifeStyle.Transient,
                Component.For<ISerializer>().ImplementedBy<BinarySerializer>(),
                Component.For<TimeoutManager>(),
                Component.For<ITransport>().ImplementedBy<MsmqTransport>().OnCreate(initTransport));
        }

        private void initTransport(IKernel kernel, ITransport transport)
        {
            transport.Init(m_path);
        }

        public void Init(string path)
        {
            m_path = path;
        }
    }
}