using Microsoft.AspNetCore.Mvc;
using Campus_SMS.ViewModels;
using Campus_SMS.Data; // for ApplicationDbContext
using System.Linq;

namespace Campus_SMS.Controllers
{
    public class AdminController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        public IActionResult Index(AdminDashboardViewModel adminDashboardViewModel)
        {
            ArgumentNullException.ThrowIfNull(adminDashboardViewModel);

            AdminDashboardViewModel model = new()
            {
   
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
