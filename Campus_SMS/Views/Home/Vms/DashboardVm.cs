namespace Campus_SMS.Views.Home.Vms
{
    public class DashboardVm
    {
        public Dictionary<string, int> CourseSmsCount { get; set; } = [];
        public Dictionary<string, int> CourseEscalationCount { get; set; } = [];

        public DashboardVm(Dictionary<string, int> courseSmsCount, Dictionary<string, int> courseEscalationCount)
        {
            this.CourseSmsCount = courseSmsCount;
            this.CourseEscalationCount = courseEscalationCount;
        }
    }
}
