using System;

namespace Messageless.Messaging
{
    internal static class Utils
    {
        public static bool IsDelegate(this Type type)
        {
            return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
        }
    }
}