using Castle.MicroKernel;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Castle.Facilities.Startable;

namespace Messageless
{
    public class MessagelessFacility : AbstractFacility
    {
        private string m_path;

        protected override void Init()
        {
            Kernel.AddFacility<StartableFacility>();
            Kernel.Register(
                Component.For<IMessageHandler>().ImplementedBy<MessageHandler>().Start(),
                Component.For<MessagelessInterceptor>().LifeStyle.Transient,
                Component.For<ISerializer>().ImplementedBy<BinarySerializer>(),
                Component.For<ITransport>().ImplementedBy<MsmqTransport>().OnCreate(initTransport));
        }

        private void initTransport(IKernel kernel, ITransport transport)
        {
            transport.Init(m_path);
        }

        internal void Init(string path)
        {
            m_path = path;
        }
    }
}