using Microsoft.AspNetCore.Mvc;
using Campus_SMS.Data; // for ApplicationDbContext
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Campus_SMS.Views.Admin.Vms;

namespace Campus_SMS.Controllers
{
    public class AdminController(ApplicationDbContext context, ILogger<AdminController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<AdminController> _logger = logger;

        public async Task<IActionResult> Index(AdminDashboardViewModel adminDashboardViewModel)
        {
            AdminDashboardViewModel model = new()
            {
                Classes = await _context.Courses.ToListAsync(),
                Faculty = await _context.Users.ToListAsync(),
                SmsInteractions = await _context.SmsInteractions.ToListAsync(),
                Announcements = await _context.Announcements.ToListAsync(),
                AIDocuments = await _context.OpenAIUploadedDocs.ToListAsync(),
                SmsUsers = await _context.SmsUsers.ToListAsync(),
                ClassProfessorMappings = await _context.ClassProfessorMappings.ToListAsync()
            };

            return View(model);
        }

        public IActionResult ManageStudents()
        {
            
            return View();
        }

        public IActionResult ManageFaculty()
        {
            return View();
        }

        public IActionResult ManageCourses()
        {
            return View();
        }

        public IActionResult ManageDocuments()
        {
            return View();
        }

        public IActionResult ViewAnalytics()
        {
            return View();
        }
    }
}
