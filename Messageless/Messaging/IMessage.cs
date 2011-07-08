using System.Reflection;

namespace Messageless.Messaging
{
    public interface IMessage
    {
        object[] Arguments { get; }
        MethodInfo Method { get; }
        Context Context { get; set; }
    }
}