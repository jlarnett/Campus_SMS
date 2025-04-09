using Campus_SMS.Data;
using Campus_SMS.Dto;
using Campus_SMS.Entities;
using Campus_SMS.Entities.User;
using Campus_SMS.Migrations;
using Campus_SMS.Views.ClassCourses.Vms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenAI.Examples;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig.Logging;

namespace Campus_SMS.Controllers
{
    public class ClassCoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly AiServiceVectorStore _AiService;


        public ClassCoursesController(ApplicationDbContext context, UserManager<AppUser> userManager, AiServiceVectorStore aiService)
        {
            _context = context;
            _userManager = userManager;
            _AiService = aiService;
        }

        // GET: ClassCourses
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses.ToListAsync();

            foreach (var course in courses)
            {
                var faculty = await _context.ClassProfessorMappings.Where(c => c.ClassCourseId.Equals(course.Id)).Include(c => c.AppUser).ToListAsync();
                foreach (var member in faculty)
                {
                    course.AppUsers.Add(member.AppUser);
                }
            }

            return View(courses);
        }

        // GET: ClassCourses/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classCourse = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (classCourse == null)
            {
                return NotFound();
            }

            return View(classCourse);
        }

        // GET: ClassCourses/Documents/5
        [Authorize]
        public async Task<IActionResult> Documents(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classCourse = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classCourse == null)
            {
                return NotFound();
            }

            return View(classCourse);
        }

        // GET: ClassCourses/ChatLog/5
        [Authorize]
        public async Task<IActionResult> ChatLog(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classCourse = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classCourse == null)
            {
                return NotFound();
            }

            var smsInteractions =
                await _context.SmsInteractions
                    .Where(c => c.CourseId.Equals(classCourse.Id))
                    .ToListAsync();

            return View(new ChatLogVm() {Class = classCourse, Log = smsInteractions});
        }

        // GET: ClassCourses/Create
        [Authorize]
        public IActionResult Create()
        {
            List<AppUserCheckboxViewModel> userCheckboxVm = new List<AppUserCheckboxViewModel>();
            var users = _userManager.Users;

            foreach (var user in users)
            {
                userCheckboxVm.Add(new AppUserCheckboxViewModel()
                {
                    Id = user.Id,
                    Name = user.Email,
                    IsChecked = false
                });
            }

            var dto = new ClassCourseDto()
            {
                AppUserIds = userCheckboxVm.ToArray()
            };

            return View(dto);
        }

        //generates a random unique join key
        private async Task<string> GenerateUniqueJoinKeyAsync(string usiIdentifier)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            string randomPart;
            string joinKey;

            do
            {
                randomPart = new string(Enumerable.Repeat(chars, 4)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                joinKey = $"{usiIdentifier}-{randomPart}";

            } while (await _context.Courses.AnyAsync(c => c.JoinKey == joinKey));

            return joinKey;
        }

        // POST: ClassCourses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ClassDescription,UsiClassIdentifier,AppUserIds")] ClassCourseDto classCourseDto)
        {
            if (ModelState.IsValid)
            {
                var JoinKey = await GenerateUniqueJoinKeyAsync(classCourseDto.UsiClassIdentifier); // Generate the JoinKey
                var classCourse = new ClassCourse()
                {
                    UsiClassIdentifier = classCourseDto.UsiClassIdentifier,
                    ClassDescription = classCourseDto.ClassDescription,
                    JoinKey = JoinKey,
                    CourseDocuments = "Documents/" + JoinKey
                };

                _context.Add(classCourse);
                var result = await _context.SaveChangesAsync();

                // Get the current working directory
                string currentDirectory = Directory.GetCurrentDirectory();

                // Define the path for the "Documents" folder
                string FolderPath = Path.Combine(currentDirectory, "Documents");

                //Path for course documents
                string newFolderPath = Path.Combine(currentDirectory, classCourse.CourseDocuments);

                // If does not exist make it.
                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }

                // Check if the "Documents" folder exists
                if (Directory.Exists(FolderPath))
                {
                    // Create the new course material folder
                    Directory.CreateDirectory(newFolderPath);
                }
                

                if (result > 0 && classCourseDto.AppUserIds.Any(c => c.IsChecked))
                {
                    foreach (var users in classCourseDto.AppUserIds)
                    {
                        if (users.IsChecked)
                            await _context.ClassProfessorMappings.AddAsync(new ClassProfessor()
                            {
                                AppUserId = users.Id,
                                ClassCourseId = classCourse.Id
                            });
                    }

                    var professorMappingResult = await _context.SaveChangesAsync();

                    if (professorMappingResult == 0)
                        return BadRequest();
                }


                return RedirectToAction(nameof(Index));
            }
            return View(classCourseDto);
        }

        // GET: ClassCourses/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classCourse = await _context.Courses.FindAsync(id);

            if (classCourse == null)
            {
                return NotFound();
            }

            List<AppUserCheckboxViewModel> userCheckboxVm = new List<AppUserCheckboxViewModel>();
            var users = await _userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                userCheckboxVm.Add(new AppUserCheckboxViewModel()
                {
                    Id = user.Id,
                    Name = user.Email ?? string.Empty,
                    IsChecked = await _context.ClassProfessorMappings.AnyAsync(c => c.AppUserId != null && c.ClassCourseId.Equals(id) && c.AppUserId.Equals(user.Id))
                });
            }

            var dto = new ClassCourseDto()
            {
                Id = classCourse.Id,
                ClassDescription = classCourse.ClassDescription,
                UsiClassIdentifier = classCourse.UsiClassIdentifier,
                CourseDocuments = classCourse.CourseDocuments,
                JoinKey = classCourse.JoinKey,
                AppUserIds = userCheckboxVm.ToArray()
            };
            return View(dto);
        }

        // POST: ClassCourses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClassDescription,UsiClassIdentifier,AppUserIds,CourseDocuments,JoinKey")] ClassCourseDto classCourseDto)
        {

            if (id != classCourseDto.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var classCourse = new ClassCourse()
                    {
                        Id = classCourseDto.Id,
                        ClassDescription = classCourseDto.ClassDescription,
                        UsiClassIdentifier = classCourseDto.UsiClassIdentifier,
                        CourseDocuments = classCourseDto.CourseDocuments,
                        JoinKey = classCourseDto.JoinKey ?? ""
                    };

                    _context.Update(classCourse);
                     var result = await _context.SaveChangesAsync();

                        foreach (var user in classCourseDto.AppUserIds)
                        {
                            if (user.IsChecked)
                            {
                                if (!await _context.ClassProfessorMappings.AnyAsync(c =>
                                        c.ClassCourseId.Equals(classCourseDto.Id) && c.AppUserId.Equals(user.Id)))
                                {
                                    await _context.ClassProfessorMappings.AddAsync(new()
                                    {
                                        AppUserId = user.Id,
                                        ClassCourseId = classCourseDto.Id
                                    });

                                    await _context.SaveChangesAsync();
                                }
                            }
                            else
                            {
                                if(await _context.ClassProfessorMappings.AnyAsync(c => c.ClassCourseId.Equals(classCourseDto.Id) && c.AppUserId.Equals(user.Id)))
                                {
                                    var mapping = await _context.ClassProfessorMappings.Where(c =>
                                        c.AppUserId.Equals(user.Id) && c.ClassCourseId.Equals(classCourseDto.Id)).FirstAsync();

                                    _context.Remove(mapping);
                                    var professorMappingResult = await _context.SaveChangesAsync();
                                    if (professorMappingResult < 1)
                                        return BadRequest();
                                }
                            }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassCourseExists(classCourseDto.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(classCourseDto);
        }

        // GET: ClassCourses/Delete/5

        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classCourse = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (classCourse == null)
            {
                return NotFound();
            }

            return View(classCourse);
        }

        // POST: ClassCourses/Delete/5
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classCourse = await _context.Courses.FindAsync(id);

            // Set CourseId to null in SmsInteractions before deleting the course
            var interactions = _context.SmsInteractions.Where(si => si.CourseId == id);
            foreach (var interaction in interactions)
            {
                interaction.CourseId = null;
            }            
            await _context.SaveChangesAsync();

            var mappings = _context.ClassProfessorMappings.Where(m => m.ClassCourseId == id);
            foreach(var mapping in mappings)
            {
                _context.ClassProfessorMappings.RemoveRange(mapping);
            }
            await _context.SaveChangesAsync();

            // Get the current working directory
            string currentDirectory = Directory.GetCurrentDirectory();

            // Define the path for the "Documents" folder
            string FolderPath = Path.Combine(currentDirectory, "Documents");

            //Path for course documents
            string newFolderPath = Path.Combine(currentDirectory, classCourse.CourseDocuments);

            string folderName = new DirectoryInfo(newFolderPath).Name;

            // Check if the folder exists
            if (Directory.Exists(newFolderPath))
            {
                foreach (string filePath in Directory.GetFiles(newFolderPath))
                {
                    string fileName = Path.GetFileName(filePath);
                    Console.WriteLine($"[DEBUG] File {fileName} attepting to be deleted.");
                    var file = _context.OpenAIUploadedDocs.FirstOrDefault(f => f.DocumentName == fileName && f.CourseFolder == folderName);
                    if (file != null)
                    {
                        System.IO.File.Delete(filePath);
                        await _AiService.DeleteDocumentsOpenAI(file.DocumentID, classCourse.CourseDocuments, classCourse.JoinKey);
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] File in {folderName} not found!");
                    }
                }
                // Delete the "newFolder" folder and its contents
                Directory.Delete(newFolderPath, true);
            }

            await _AiService.DeleteAssistent(classCourse.AssistentId);

            if (classCourse != null)
            {
                _context.Courses.Remove(classCourse);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: ClassCourses/BlockNumber
        [Authorize]
        public async Task<IActionResult> BlockNumber(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classCourse = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classCourse == null)
            {
                return NotFound();
            }

            return View(classCourse);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBlockedNumber(int id, string phoneNumber)
        {
            var classCourse = await _context.Courses.FindAsync(id);
            if (classCourse == null)
            {
                return NotFound();
            }

            foreach (var number in classCourse.BlockedNumbers.ToList())
            {
                if (number == phoneNumber)
                {
                    classCourse.BlockedNumbers.Remove(number);
                }
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(BlockNumber), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBlockedNumber(int id, string phoneNumber)
        {
            var classCourse = await _context.Courses.FindAsync(id);
            if (classCourse == null)
            {
                return NotFound();
            }

            if(classCourse.BlockedNumbers == null)
            {
                classCourse.BlockedNumbers = [];
            }

            classCourse.BlockedNumbers.Add("+"+phoneNumber);
            
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(BlockNumber), new { id });
        }

        // POST: ClassCourses/UploadFile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Retrieve the course from the database
            var classCourse = await _context.Courses.FindAsync(id);
            if (classCourse == null)
            {
                return NotFound();
            }

            // Get the current working directory
            string currentDirectory = Directory.GetCurrentDirectory();

            // Define the path to store the file
            string courseFolderPath = Path.Combine(currentDirectory, classCourse.CourseDocuments);

            if (Directory.Exists(courseFolderPath))
            {
                // Generate a filename for the file
                string fileName = Path.GetFileName(file.FileName);
                string filePath = Path.Combine(courseFolderPath, fileName);

                // Save the file to the disk
                // Check if file already exists for this course
                string courseFolderName = new DirectoryInfo(courseFolderPath).Name;
                bool exists = await _context.OpenAIUploadedDocs.AnyAsync(d => d.DocumentName == fileName && d.CourseFolder == courseFolderName);
                if (!exists)
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
                await _AiService.CreateAssistent(classCourse.JoinKey, courseFolderPath);
            }

            return RedirectToAction(nameof(Index));  // Redirect to the index page after upload
        }

        public async Task<IActionResult> DeleteFile(int id, string docName)
        {
            // Retrieve the course from the database
            var classCourse = await _context.Courses.FindAsync(id);
            if (classCourse == null)
            {
                Console.WriteLine("No Class Found");
                return NotFound();
            }

            Console.WriteLine("[DEBUG] The file name is: ", docName);

            string currentDirectory = Directory.GetCurrentDirectory();
            string courseFolderPath = Path.Combine(currentDirectory, classCourse.CourseDocuments);
            string courseFolderName = new DirectoryInfo(courseFolderPath).Name;

            var file = await _context.OpenAIUploadedDocs
                .FirstOrDefaultAsync(f => f.DocumentName == docName && f.CourseFolder == courseFolderName);
            if (file == null)
            {
                Console.WriteLine("No File Found");
                return NotFound();
            }

            if (Directory.Exists(courseFolderPath))
            {
                // Generate a filename for the file
                string fileName = Path.GetFileName(file.DocumentName);
                string filePath = Path.Combine(courseFolderPath, fileName);

                // Delete the file to the disk
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    await _AiService.DeleteDocumentsOpenAI(file.DocumentID, classCourse.CourseDocuments, classCourse.JoinKey);
                }
            }

            return RedirectToAction(nameof(Index));  // Redirect to the index page after upload
        }

        [HttpGet]
        public async Task<IActionResult> CheckFileExists(string fileName, int id)
        {
            // Retrieve the course from the database
            var classCourse = await _context.Courses.FindAsync(id);
            if (classCourse == null)
            {
                Console.WriteLine("No Class Found");
                return NotFound();
            }

            string currentDirectory = Directory.GetCurrentDirectory();
            string courseFolderPath = Path.Combine(currentDirectory, classCourse.CourseDocuments);
            string courseFolderName = new DirectoryInfo(courseFolderPath).Name;

            bool exists = await _context.OpenAIUploadedDocs.AnyAsync(d => d.DocumentName == fileName && d.CourseFolder == courseFolderName);

            return Json(new { exists });
        }


        private bool ClassCourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
