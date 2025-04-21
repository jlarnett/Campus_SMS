namespace Campus_SMS.Models
{
    public class Permission
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;  // non-nullable with default
        public bool IsEnabled { get; set; }
    }
}
