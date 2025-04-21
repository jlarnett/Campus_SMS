using Campus_SMS.Entities;
using Campus_SMS.Entities.User;

namespace Campus_SMS.Views.Admin.Vms;

public class AdminDashboardViewModel()
{
    public List<ClassCourse> Classes { get; set; } = [];
    public List<AppUser> Faculty { get; set; } = [];
    public List<SmsInteraction> SmsInteractions { get; set; } = [];
    public List<Announcement> Announcements { get; set; } = [];
    public List<OpenAIUploadedDocs> AIDocuments { get; set; } = [];
    public List<SMSUser> SmsUsers { get; set; } = [];
    public List<ClassProfessor> ClassProfessorMappings { get; set; } = [];



}
