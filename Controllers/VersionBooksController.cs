using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibApp.Data;
using LibApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace LibApp.Controllers
{
    [Authorize(Roles="Admin, Librarian")]
    public class VersionBooksController : Controller
    {
        private readonly AppDbContext _context;

        public VersionBooksController(AppDbContext context)
        {
            _context = context;
        }

        // GET: VersionBooks
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.VersionBooks.Include(v => v.Book).Include(v => v.Publisher);
            return View(await appDbContext.ToListAsync());
        }

        // GET: VersionBooks/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var versionBook = await _context.VersionBooks
                .Include(v => v.Book)
                .Include(v => v.Publisher)
                .FirstOrDefaultAsync(m => m.VersionBookId == id);
            if (versionBook == null)
            {
                return NotFound();
            }

            return View(versionBook);
        }

        // GET: VersionBooks/Create
        public IActionResult Create()
        {
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Name");
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "PublisherId", "Name");
            return View();
        }

        // POST: VersionBooks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VersionBookId,PublisherId,BookId,Name,CreateAt,CountSheets")] VersionBook versionBook)
        {
            if (ModelState.IsValid)
            {
                versionBook.CreateAt = DateTime.SpecifyKind(versionBook.CreateAt, DateTimeKind.Utc);

                _context.Add(versionBook);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Name", versionBook.BookId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "PublisherId", "Name", versionBook.PublisherId);
            return View(versionBook);
        }

        // GET: VersionBooks/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var versionBook = await _context.VersionBooks.FindAsync(id);
            if (versionBook == null)
            {
                return NotFound();
            }
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Name", versionBook.BookId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "PublisherId", "Name", versionBook.PublisherId);
            return View(versionBook);
        }

        // POST: VersionBooks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("VersionBookId,PublisherId,BookId,Name,CreateAt,CountSheets")] VersionBook versionBook)
        {
            if (id != versionBook.VersionBookId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    versionBook.CreateAt = DateTime.SpecifyKind(versionBook.CreateAt, DateTimeKind.Utc);
                    _context.Update(versionBook);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VersionBookExists(versionBook.VersionBookId))
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
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Name", versionBook.BookId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "PublisherId", "Name", versionBook.PublisherId);
            return View(versionBook);
        }

        // GET: VersionBooks/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var versionBook = await _context.VersionBooks
                .Include(v => v.Book)
                .Include(v => v.Publisher)
                .FirstOrDefaultAsync(m => m.VersionBookId == id);
            
            var hasRelations = await _context.ExampleBooks.AnyAsync(m => m.VersionBookId == id);

            if (versionBook.Book != null || versionBook.Publisher != null || hasRelations)
            {
                ViewBag.ErrorMessage = "Нарушение связей - удалить нельзя";
                return View("Delete",versionBook);
            }
            if (versionBook == null)
            {
                return NotFound();
            }

            return View(versionBook);
        }

        // POST: VersionBooks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var versionBook = await _context.VersionBooks
                .Include(v => v.Book)
                .Include(v => v.Publisher)
                .FirstOrDefaultAsync(m => m.VersionBookId == id);
            
            var hasRelations = await _context.ExampleBooks.AnyAsync(m => m.VersionBookId == id);

            if (versionBook.Book != null || versionBook.Publisher != null || hasRelations)
            {
                ViewBag.ErrorMessage = "Нарушение связей - удалить нельзя";
                return View("Delete",versionBook);
            }
            if (versionBook != null)
            {
                _context.VersionBooks.Remove(versionBook);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VersionBookExists(long id)
        {
            return _context.VersionBooks.Any(e => e.VersionBookId == id);
        }
    }
}
