using Microsoft.AspNetCore.Mvc;

namespace Campus_SMS.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();

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
