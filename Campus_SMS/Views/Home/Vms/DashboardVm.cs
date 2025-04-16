namespace Campus_SMS.Views.Home.Vms
{
    public class DashboardVm
    {
        public Dictionary<string, int> CourseSmsCount { get; set; } = [];
        public Dictionary<string, int> CourseEscalationCount { get; set; } = [];
        public Dictionary<string, Dictionary<string, int>> CourseCommonWords { get; set; } = [];
        public List<DailyResponseTime> DailyResponseTimes { get; set; } = [];



        public DashboardVm(Dictionary<string, int> courseSmsCount,
            Dictionary<string, int> courseEscalationCount, Dictionary<string,
                Dictionary<string, int>> courseCommonWords, List<DailyResponseTime> responseTimes)
        {
            this.CourseSmsCount = courseSmsCount;
            this.CourseEscalationCount = courseEscalationCount;
            this.CourseCommonWords = courseCommonWords;
            this.DailyResponseTimes = responseTimes;
        }
    }

    public class DailyResponseTime
    {
        public DateTime Date { get; set; }
        public double AverageResponseTimeMilliseconds { get; set; }
    }
}
