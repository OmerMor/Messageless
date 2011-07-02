using System;

namespace Messageless
{
    [Serializable]
    public class TransportMessage
    {
        private TransportMessage()
        {
        }

        public TransportMessage(byte[] payload, string recipientPath)
        {
            Payload = payload;
            RecipientPath = recipientPath;
        }

        public byte[] Payload { get; set; }
        public string RecipientPath { get; set; }
        public string SenderPath { get; set; }
    }
}