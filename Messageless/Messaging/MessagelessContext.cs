using System;
using System.Collections.Generic;
using System.Threading;

namespace Messageless.Messaging
{
    public static class MessagelessContext
    {
        private static readonly ThreadLocal<Stack<Context>> s_contexts =
            new ThreadLocal<Stack<Context>>(() =>
            {
                var stack = new Stack<Context>();
                stack.Push(null);
                return stack;
            });

        public static Context CurrentContext
        {
            get { return s_contexts.Value.Peek(); }
        }

        public static void Execute(Action<Context> action, Context context = null)
        {
            context = context ?? new Context();
            s_contexts.Value.Push(context);
            try
            {
                action(context);
            }
            finally
            {
                s_contexts.Value.Pop();
            }
        }

        public static void Execute(Action<Context> action, TimeSpan timeout)
        {
            Execute(context =>
            {
                context.TimeOut = timeout;
                action(context);
            });
        }
    }
}