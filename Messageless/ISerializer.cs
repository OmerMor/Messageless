namespace Messageless
{
    public interface ISerializer
    {
        byte[] Serialize(object obj);
        object Deserialize(byte[] buffer);
    }
}