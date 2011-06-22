using System;

namespace Messageless
{
    [Serializable]
    public class Context
    {
        public Guid Token { get; set; }
        public string Path { get; set; }

        public Context(Guid token, string path)
        {
            Token = token;
            Path = path;
        }
    }
}