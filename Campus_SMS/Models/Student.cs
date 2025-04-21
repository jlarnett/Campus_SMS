namespace Campus_SMS.Models
{
    public class Student
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public List<Course> Classes { get; set; } = new List<Course>();
    }
}
