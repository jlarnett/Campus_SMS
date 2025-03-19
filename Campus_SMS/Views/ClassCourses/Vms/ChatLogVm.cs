using Campus_SMS.Entities;

namespace Campus_SMS.Views.ClassCourses.Vms
{
    public class ChatLogVm
    {
        public List<SmsInteraction> Log { get; set; } = [];
        public required ClassCourse Class { get; set; }
    }
}
