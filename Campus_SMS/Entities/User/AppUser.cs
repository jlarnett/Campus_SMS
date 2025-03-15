using Microsoft.AspNetCore.Identity;

namespace Campus_SMS.Entities.User
{
    public class AppUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public byte[]? ProfilePicture { get; set; }
        public string CustomTag { get; set; } = String.Empty;

        public List<ClassCourse> ClassCourses { get; } = [];
    }
}
