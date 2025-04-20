using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campus_SMS.Controllers
{
    public class AdminController : Controller
    {
        [Authorize(Roles = "admin")]
        public IActionResult Index()
        {
            return View();

        }

        [Authorize(Roles = "admin")]
        public IActionResult ManageCourses()
        {
            return View();
        }

        [Authorize(Roles = "admin")]
        public IActionResult ManageDocuments()
        {
            return View();
        }

        [Authorize(Roles = "admin")]
        public IActionResult ViewAnalytics()
        {
            return View();
        }
    }
}
