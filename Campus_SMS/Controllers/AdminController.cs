using Microsoft.AspNetCore.Mvc;
using Campus_SMS.Data; // for ApplicationDbContext
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Campus_SMS.Views.Admin.Vms;
using Campus_SMS.Models;
using DocumentFormat.OpenXml.Drawing.Charts;

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

        // Gets JSON info for a selected faculty member
        [HttpGet]
        public JsonResult GetFacultyInfo(string id)
        {
            var faculty = _context.Users.FirstOrDefault(f => f.Id == id);

            var classMapping = _context.ClassProfessorMappings
                .Where(m => m.AppUserId == id)
                .Select(m => m.ClassCourseId)
                .ToList();

            var classes = _context.Courses
                .Where(c => classMapping.Contains(c.Id))
                .Select (c => new
                {
                    classId = c.Id,
                    className =c.ClassDescription,
                    usiCode = c.UsiClassIdentifier
                })
                .ToList();

            if (faculty == null)
            {
                return Json(new { error = "Can't find faculty member" });
            }

            return Json(new
            {
                firstName = faculty.FirstName,
                lastName = faculty.LastName,
                email = faculty.Email,
                classesList = classes
            });
        }

        public JsonResult GetSMSInfo(int id)
        {
            var smsUser = _context.SmsUsers.FirstOrDefault(s => s.Id == id);

            if (smsUser == null)
            {
                return Json(new { error = "Can't find SMS user" });
            }

            return Json(new
            {
                phoneNumber = smsUser.PhoneNumber,
                enrolledCourses = smsUser.EnrolledCourses ?? new List<string>()
            });
        }

        public IActionResult RemoveEnrolledCourse([FromBody] RemoveCourseRequest request)
        {

            var user = _context.SmsUsers.FirstOrDefault(s => s.Id == request.UserId);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            if (user.EnrolledCourses == null)
            {
                return Json(new { success = false, message = "Course list is null." });
            }

            if (!user.EnrolledCourses.Contains(request.Course))
            {
                return Json(new { success = false, message = "Course not in list." });
            }

            user.EnrolledCourses.Remove(request.Course);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        public IActionResult RemoveFacultyCourse([FromBody] RemoveFacultyCourseRequest request)
        {
            try
            {
                var mapping = _context.ClassProfessorMappings
                    .FirstOrDefault(m => m.AppUserId == request.FacultyId && m.ClassCourseId == request.CourseId);

                if (mapping == null)
                    return Json(new { success = false, message = "Mapping not found" });

                _context.ClassProfessorMappings.Remove(mapping);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
