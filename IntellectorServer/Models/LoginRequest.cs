namespace IntellectorServer.Models
{
    public class LoginRequest
    {
        public string UserLogin {  get; set; }
        public string UserPassword {  get; set; }
        public string Version {  get; set; }
        public string APIKey {  get; set; }
    }
}
