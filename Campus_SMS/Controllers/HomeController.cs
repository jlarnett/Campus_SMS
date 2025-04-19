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

        public static Dictionary<string, int> FindCommonWordsAndPhrases(List<string> strings, int minPhraseLength = 3, int maxPhraseLength = 9)
        {
            var phraseCounts = new Dictionary<string, int>();

            foreach (var str in strings)
            {
                // Normalize and split the string into words
                var words = str.ToLower()
                    .Split(new[] { ' ', '\t', '\r', '\n', '.', ',', ';', ':', '!', '?', '-', '"', '\'' },
                        StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < words.Length; i++)
                {
                    for (int length = minPhraseLength; length <= maxPhraseLength && i + length <= words.Length; length++)
                    {
                        var phrase = string.Join(" ", words.Skip(i).Take(length));

                        if (phraseCounts.TryGetValue(phrase, out int value))
                        {
                            phraseCounts[phrase] = ++value;
                        }
                        else
                        {
                            phraseCounts[phrase] = 1;
                        }
                    }
                }
            }

            // Only keep phrases that occur more than once
            return phraseCounts
                .Where(kvp => kvp.Value > 1)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public async Task<IActionResult> Index()
        {
            DashboardVm vm;

            //Calculate today's date
            var today = DateTime.UtcNow.Date;
            var sevenDaysAgo = today.AddDays(-6); // includes today, 7 total days

            // Generate list of all dates between sevenDaysAgo and today (inclusive)
            var allDates = Enumerable.Range(0, (today - sevenDaysAgo).Days + 1)
                .Select(offset => sevenDaysAgo.AddDays(offset).Date)
                .ToList();

            // Get interaction data and group by date
            var groupedResponseTimes = _context.SmsInteractions
                .Where(s => s.TimeReceived.Date >= sevenDaysAgo && s.TimeReceived.Date <= today)
                .AsEnumerable()
                .GroupBy(s => s.TimeReceived.Date)
                .ToDictionary(g => g.Key, g => g.Average(s =>
                    (s.TimeResponded - s.TimeReceived).TotalMilliseconds));

            // Build the final list with defaults for missing days
            var responseTimesForWeek = allDates
                .Select(date => new DailyResponseTime
                {
                    Date = date,
                    AverageResponseTimeMilliseconds = groupedResponseTimes.TryGetValue(date, out var avg)
                        ? avg
                        : 0
                })
                .OrderBy(r => r.Date)
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
                        await _context.SmsInteractions
                            .Where(s => s.CourseId.Equals(course.ClassCourseId) && (s.IncomingSmsMessage.Contains("ESCALATE-REQ")  || s.AiSmsResponse.Contains("ESCALATE-REQ")))
                            .CountAsync();
                    courseSmsCount.Add(course.Class.UsiClassIdentifier, count);
                    courseEscalationCount.Add(course.Class.UsiClassIdentifier, escalationCount);

                    var messages = await _context.SmsInteractions.Where(p => p.CourseId.Equals(course.ClassCourseId)).Select(p => p.IncomingSmsMessage).ToListAsync();
                    var commonWords = FindCommonWordsAndPhrases(messages);
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
