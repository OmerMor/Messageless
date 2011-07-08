using Castle.MicroKernel;

namespace Messageless.IoC
{
    public interface IMessagelessFacility : IFacility
    {
        void Init(string path);
    }
}