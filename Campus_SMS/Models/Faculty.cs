using Microsoft.Graph.Models;

namespace Campus_SMS.Models
{
    public class Faculty
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty; // default value provided
        public string Email { get; set; } = string.Empty;
        public List<Permission> Permissions { get; set; } = new List<Permission>();
        public List<Course> Classes { get; set; } = new List<Course>();
    }
}
