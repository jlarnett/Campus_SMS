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
        private readonly SignInManager<AppUser> _signInManager;


        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, 
            UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public static Dictionary<string, int> FindCommonWords(List<string> strings)
        {
            // Dictionary to store word counts across all strings
            var wordCounts = new Dictionary<string, int>();

            // Tokenize each string into words and update the counts
            foreach (var str in strings)
            {
                var words = str.ToLower().Split(' ');

                foreach (var word in words)
                {
                    if (wordCounts.ContainsKey(word))
                    {
                        wordCounts[word]++;
                    }
                    else
                    {
                        wordCounts[word] = 1;
                    }
                }
            }

            // Find the words that appear in all strings
            var commonWords = wordCounts.Where(kvp => strings.All(s => s.ToLower().Contains(kvp.Key)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return commonWords;
        }

        public async Task<IActionResult> Index()
        {
            DashboardVm vm;

            //Calculate today's date
            var today = DateTime.UtcNow.Date;
            var sevenDaysAgo = today.AddDays(-6); // includes today, 7 total days

            var responseTimesForWeek = _context.SmsInteractions
                .Where(s => s.TimeReceived.Date >= sevenDaysAgo && s.TimeReceived.Date <= today)
                .AsEnumerable()
                .GroupBy(s => s.TimeReceived.Date)
                .Select(g => new DailyResponseTime
                {
                    Date = g.Key,
                    AverageResponseTimeMilliseconds = g.Average(s =>
                        (s.TimeResponded - s.TimeReceived).TotalMilliseconds)
                })
                .OrderBy(result => result.Date)
                .ToList();

            if (_signInManager.IsSignedIn(User))
            {
                var courses = await _context.ClassProfessorMappings.Where(c => c.AppUserId.Equals(_userManager.GetUserId(User))).Include(c => c.Class).ToListAsync();
                Dictionary<string, int> courseSmsCount = [];
                Dictionary<string, int> courseEscalationCount = [];
                Dictionary<string, Dictionary<string, int>> courseCommonWords = [];

                foreach (var course in courses)
                {
                    var count = await _context.SmsInteractions.Where(s => s.CourseId.Equals(course.ClassCourseId)).CountAsync();
                    var escalationCount =
                        await _context.SmsInteractions.Where(s => s.CourseId.Equals(course.ClassCourseId) && s.IncomingSmsMessage.Contains("escalate")).CountAsync();
                    courseSmsCount.Add(course.Class.UsiClassIdentifier, count);
                    courseEscalationCount.Add(course.Class.UsiClassIdentifier, escalationCount);

                    var messages = await _context.SmsInteractions.Where(p => p.CourseId.Equals(course.ClassCourseId)).Select(p => p.IncomingSmsMessage).ToListAsync();
                    var commonWords = FindCommonWords(messages);
                    courseCommonWords.Add(course.Class.UsiClassIdentifier, commonWords);
                }

                vm = new DashboardVm(courseSmsCount, courseEscalationCount, courseCommonWords, responseTimesForWeek);
            }
            else
            {
                vm = new DashboardVm(new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, Dictionary<string, int>>(), responseTimesForWeek);
            }

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
