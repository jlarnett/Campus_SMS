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

        // GET: ClassCourses/Create
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

        // POST: ClassCourses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ClassDescription,UsiClassIdentifier,AppUserIds")] ClassCourseDto classCourseDto)
        {
            if (ModelState.IsValid)
            {
                var classCourse = new ClassCourse()
                {
                    UsiClassIdentifier = classCourseDto.UsiClassIdentifier,
                    ClassDescription = classCourseDto.ClassDescription,
                };

                _context.Add(classCourse);
                var result = await _context.SaveChangesAsync();

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
            var users = _userManager.Users;

            foreach (var user in users)
            {
                userCheckboxVm.Add(new AppUserCheckboxViewModel()
                {
                    Id = user.Id,
                    Name = user.Email,
                    IsChecked = await _context.ClassProfessorMappings.AnyAsync(c => c.ClassCourseId.Equals(id) && c.AppUserId.Equals(user.Id))
                });
            }

            var dto = new ClassCourseDto()
            {
                Id = classCourse.Id,
                ClassDescription = classCourse.ClassDescription,
                UsiClassIdentifier = classCourse.UsiClassIdentifier,
                AppUserIds = userCheckboxVm.ToArray()
            };

            return View(dto);
        }

        // POST: ClassCourses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClassDescription,UsiClassIdentifier,AppUserIds")] ClassCourseDto classCourseDto)
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
                        UsiClassIdentifier = classCourseDto.UsiClassIdentifier
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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classCourse = await _context.Courses.FindAsync(id);
            if (classCourse != null)
            {
                _context.Courses.Remove(classCourse);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClassCourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
