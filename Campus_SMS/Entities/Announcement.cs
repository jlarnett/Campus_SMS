using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Campus_SMS.Entities
{
    public class Announcement
    {
        public int Id { get; set; }

        [MaxLength(160)]
        [DisplayName("Outbound Message")]
        public string OutboundMessage { get; set; }

        [DisplayName("Selected Course")]
        public int CourseId { get; set; }
        public ClassCourse? Course { get; set; }
    }
}
