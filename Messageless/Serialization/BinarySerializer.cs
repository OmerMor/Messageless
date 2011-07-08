using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Messageless.Serialization
{
    public class BinarySerializer : ISerializer
    {
        private readonly IFormatter m_formatter = new BinaryFormatter();

        public byte[] Serialize(object obj)
        {
            var stream = new MemoryStream();
            m_formatter.Serialize(stream, obj);
            var buffer = stream.ToArray();

            return buffer;
        }

        public object Deserialize(byte[] buffer)
        {
            var stream = new MemoryStream(buffer);
            var obj = m_formatter.Deserialize(stream);

            return obj;
        }
    }
}