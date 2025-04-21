namespace Campus_SMS.Models
{
    public class Course
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string InstructorName { get; set; }
        public string InstructorEmail { get; set; }
        public int EnrolledStudentCount { get; set; }

        // The index view references `@Model.SelectedCourse.Analytics`
        public AnalyticsData Analytics { get; set; }
    }
}
