using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Campus_SMS.Data;
using Campus_SMS.Dto;
using Campus_SMS.Entities;
using Campus_SMS.Entities.User;
using Campus_SMS.Views.ClassCourses.Vms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Campus_SMS.Controllers
{
    public class ClassCoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;


        public ClassCoursesController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

                // Check if the "Documents" folder already exists
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
                        JoinKey = classCourseDto.JoinKey
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

            await _context.SaveChangesAsync(); // Save changes before deleting course

            if (classCourse != null)
            {
                _context.Courses.Remove(classCourse);
            }

            await _context.SaveChangesAsync();

            // Get the current working directory
            string currentDirectory = Directory.GetCurrentDirectory();

            // Define the path for the "Documents" folder
            string FolderPath = Path.Combine(currentDirectory, "Documents");

            //Path for course documents
            string newFolderPath = Path.Combine(currentDirectory, classCourse.CourseDocuments);

            // Check if the "Documents" folder exists
            if (Directory.Exists(FolderPath))
            {
                // Delete the "newFolder" folder and its contents
                Directory.Delete(newFolderPath, true);
            }

            return RedirectToAction(nameof(Index));
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
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            return RedirectToAction(nameof(Index));  // Redirect to the index page after upload
        }


        private bool ClassCourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
