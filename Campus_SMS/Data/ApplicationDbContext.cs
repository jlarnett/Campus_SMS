using Campus_SMS.Entities;
using Campus_SMS.Entities.User;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Campus_SMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SmsInteraction> SmsInteractions { get; set; }
        public DbSet<ClassCourse> Courses { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<ClassProfessor> ClassProfessorMappings { get; set; }



    }
}
