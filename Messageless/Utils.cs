using System;

namespace Messageless
{
    internal static class Utils
    {
        public static bool IsDelegate(this Type type)
        {
            return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
        }
    }
}