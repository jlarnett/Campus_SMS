using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Campus_SMS.Entities.User;

namespace Campus_SMS.Entities
{
    public class ClassCourse
    {
        public int Id { get; set; }

        [DisplayName("Description")]
        [MaxLength(500)]
        public string ClassDescription { get; set; }

        [DisplayName("USI Class Identifier Code")]
        [MaxLength(10)]
        public string UsiClassIdentifier { get; set; }

        public List<AppUser> AppUsers { get; set; } = [];
    }
}
