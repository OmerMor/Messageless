namespace Messageless.Serialization
{
    public interface ISerializer
    {
        byte[] Serialize(object obj);
        object Deserialize(byte[] buffer);
    }
}