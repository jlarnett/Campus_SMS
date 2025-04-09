using Campus_SMS.Entities;

namespace Campus_SMS.Views.ClassCourses.Vms
{
    public class ChatLogVm
    {
        public ClassCourse Class { get; set; }
        public Dictionary<string, List<SmsInteraction>> GroupedLogs { get; set; }
    }
}
