using System.ComponentModel.DataAnnotations;

namespace Campus_SMS.Entities
{
    public class Announcement
    {
        public int Id { get; set; }

        [MaxLength(160)]
        public string OutboundMessage { get; set; }

        public int CourseId { get; set; }
        public ClassCourse? Course { get; set; }
    }
}
