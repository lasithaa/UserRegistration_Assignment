namespace UserRegistrationApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string IdentityCardNumber { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? OtpExpirationTime { get; set; }
    }

}
