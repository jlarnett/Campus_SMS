using System.ComponentModel.DataAnnotations;
using Campus_SMS.Entities.User;

namespace Campus_SMS.Entities
{
    public class ClassProfessor
    {
        public int Id { get; set; }

        //Faculty & Users
        public string? AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        //Classes
        public int? ClassCourseId { get; set; }
        public ClassCourse Class { get; set; } = null!;
    }
}
