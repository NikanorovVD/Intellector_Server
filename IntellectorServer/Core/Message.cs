namespace IntellectorServer.Core
{
    public class Message
    {
        public string Method { get; set; }
        public string Body { get; set; }

        public override string ToString()
        {
            return $"{Method}: {Body}";
        }
    }
}
