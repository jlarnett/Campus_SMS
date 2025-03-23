using Campus_SMS.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Campus_SMS.Data;
using Campus_SMS.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Campus_SMS.Views.Home.Vms;
namespace Campus_SMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;


        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var courses = await _context.ClassProfessorMappings.Where(c => c.AppUserId.Equals(_userManager.GetUserId(User))).Include(c => c.Class).ToListAsync();
            Dictionary<string, int> courseSmsCount = [];
            Dictionary<string, int> courseEscalationCount = [];

            foreach (var course in courses)
            {
                var count = await _context.SmsInteractions.Where(s => s.CourseId.Equals(course.ClassCourseId)).CountAsync();
                var escalationCount =
                    await _context.SmsInteractions.Where(s => s.CourseId.Equals(course.ClassCourseId) && s.IncomingSmsMessage.Contains("escalate")).CountAsync();
                courseSmsCount.Add(course.Class.UsiClassIdentifier, count);
                courseEscalationCount.Add(course.Class.UsiClassIdentifier, escalationCount);
            }

            DashboardVm vm = new DashboardVm(courseSmsCount, courseEscalationCount);
            return View(vm);
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
