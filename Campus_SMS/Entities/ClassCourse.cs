using System.ComponentModel.DataAnnotations;

namespace Campus_SMS.Entities
{
    public class ClassCourse
    {
        public int Id { get; set; }

        [MaxLength(500)]
        public string ClassDescription { get; set; }

        [MaxLength(10)]
        public string UsiClassIdentifier { get; set; }
    }
}
