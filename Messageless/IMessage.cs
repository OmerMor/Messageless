using System.Reflection;

namespace Messageless
{
    public interface IMessage
    {
        object[] Arguments { get; }
        MethodInfo Method { get; }
    }
}