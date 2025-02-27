using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Campus_SMS.Entities.User;

namespace Campus_SMS.Dto
{
    public class ClassCourseDto
    {
        public int Id { get; set; }

        [DisplayName("Description")]
        [MaxLength(500)]
        public string ClassDescription { get; set; }

        [DisplayName("USI Class Identifier Code")]
        [MaxLength(10)]
        public string UsiClassIdentifier { get; set; }

        public string CourseDocuments { get; set; }

        public AppUserCheckboxViewModel[] AppUserIds { get; set; } = [];
    }

    public class AppUserCheckboxViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
    }
}
