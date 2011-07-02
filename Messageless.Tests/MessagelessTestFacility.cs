using Castle.Facilities.Startable;
using Castle.MicroKernel;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;

namespace Messageless.Tests
{
    public class MessagelessTestFacility<TTransport> : AbstractFacility, IMessagelessFacility 
        where TTransport : ITransport
    {
        private string m_path;

        protected override void Init()
        {
            Kernel.AddFacility<StartableFacility>();
            Kernel.Register(
                Component.For<IMessageHandler>().ImplementedBy<MessageHandler>().Start(),
                Component.For<InvocationInterceptor>().LifeStyle.Transient,
                Component.For<ISerializer>().ImplementedBy<BinarySerializer>(),
                Component.For<ITransport>().ImplementedBy<TTransport>().OnCreate(initTransport));
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