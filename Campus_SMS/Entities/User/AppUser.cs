using Microsoft.AspNetCore.Identity;

namespace Campus_SMS.Entities.User
{
    public class AppUser : IdentityUser
    {
        public string CustomTag { get; set; } = String.Empty;
    }
}
