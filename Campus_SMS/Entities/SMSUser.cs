namespace Campus_SMS.Entities
{
    public class SMSUser
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsFirstTime { get; set; }
        public bool OptStatus { get; set; }
        public string? CurrentCourse { get; set; }
    }
}
