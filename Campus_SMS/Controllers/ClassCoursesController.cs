using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Campus_SMS.Data;
using Campus_SMS.Entities;

namespace Campus_SMS.Controllers
{
    public class ClassCoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassCoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ClassCourses
        public async Task<IActionResult> Index()
        {
            return View(await _context.Courses.ToListAsync());
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
            return View();
        }

        // POST: ClassCourses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ClassDescription,UsiClassIdentifier")] ClassCourse classCourse)
        {
            if (ModelState.IsValid)
            {
                _context.Add(classCourse);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(classCourse);
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
            return View(classCourse);
        }

        // POST: ClassCourses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClassDescription,UsiClassIdentifier")] ClassCourse classCourse)
        {
            if (id != classCourse.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(classCourse);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassCourseExists(classCourse.Id))
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
            return View(classCourse);
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
