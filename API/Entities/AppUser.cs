using Microsoft.AspNetCore.Identity;

namespace API.Entities
{
    public class AppUser : IdentityUser
    {
        public required string DisplayName { get; set; }
        public string? ImageUrl { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public string? EmailConfirmationCode { get; set; }
        public DateTime? EmailConfirmationCodeExpiry { get; set; }
        public string? SmsConfirmationCode { get; set; }
        public DateTime? SmsConfirmationCodeExpiry { get; set; }


        //Nav Property

        public Member Member { get; set; } = null!;
    }
}