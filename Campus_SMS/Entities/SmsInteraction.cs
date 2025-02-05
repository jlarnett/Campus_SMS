using System.ComponentModel.DataAnnotations;

namespace Campus_SMS.Entities
{
    public class SmsInteraction
    {
        public int Id { get; set; }

        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        [MaxLength(2000)]
        public string IncomingSmsMessage { get; set; }

        [MaxLength(160)]
        public string AiSmsResponse { get; set; }

        public DateTime TimeReceived { get; set; }

        public DateTime TimeResponded { get; set; }

        public int? CourseId { get; set; }
        public ClassCourse? Course { get; set; }

    }
}
