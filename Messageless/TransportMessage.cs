using System;

namespace Messageless
{
    [Serializable]
    public class TransportMessage
    {
        private TransportMessage()
        {
        }

        public TransportMessage(byte[] payload, string path, string key)
        {
            Payload = payload;
            Path = path;
            Key = key;
        }

        public byte[] Payload { get; set; }
        public string Path { get; set; }
        public string Key { get; set; }
        public string SenderPath { get; set; }
    }
}