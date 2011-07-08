using System;

namespace Messageless.Messaging
{
    [Serializable]
    public class Context
    {
        public string RecipientPath { get; set; }
        public string RecipientKey { get; set; }
        public string SenderPath { get; set; }

        public TimeSpan TimeOut { get; set; }

        public bool CallbackTimedOut { get; set; }
    }
}