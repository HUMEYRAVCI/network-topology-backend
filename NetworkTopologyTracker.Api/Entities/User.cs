namespace NetworkTopologyTracker.Api.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Güvenli şekilde hash'lenmiş şifre
        public string Role { get; set; } = string.Empty; // "SuperAdmin" veya "Viewer"
    }
}