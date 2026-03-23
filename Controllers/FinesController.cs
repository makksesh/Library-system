
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibApp.Data;
using LibApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace LibApp.Controllers
{
    [Authorize(Roles="Admin, Librarian")]
    public class FinesController : Controller
    {
        private readonly AppDbContext _context;

        public FinesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Fines
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Fines.Include(f => f.Librarian).Include(f => f.Reader);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Fines/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fine = await _context.Fines
                .Include(f => f.Librarian)
                .Include(f => f.Reader)
                .FirstOrDefaultAsync(m => m.FineId == id);
            if (fine == null)
            {
                return NotFound();
            }

            return View(fine);
        }
        
        public IActionResult Create()
        {
            ViewData["LibrarianId"] = new SelectList(_context.Users, "UserId", "Email");
            ViewData["ReaderId"] = new SelectList(_context.Users, "UserId", "Email");
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FineId,ReaderId,LibrarianId,Amount,IssuedAt,PaidAt,Reason")] Fine fine)
        {
            if (ModelState.IsValid)
            {
                fine.IssuedAt = DateTime.SpecifyKind(fine.IssuedAt, DateTimeKind.Utc);

                if (fine.PaidAt.HasValue)
                    fine.PaidAt = DateTime.SpecifyKind(fine.PaidAt.Value, DateTimeKind.Utc);
                
                _context.Add(fine);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["LibrarianId"] = new SelectList(_context.Users, "UserId", "Email", fine.LibrarianId);
            ViewData["ReaderId"] = new SelectList(_context.Users, "UserId", "Email", fine.ReaderId);
            return View(fine);
        }
        
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fine = await _context.Fines.FindAsync(id);
            if (fine == null)
            {
                return NotFound();
            }
            ViewData["LibrarianId"] = new SelectList(_context.Users, "UserId", "Email", fine.LibrarianId);
            ViewData["ReaderId"] = new SelectList(_context.Users, "UserId", "Email", fine.ReaderId);
            return View(fine);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("FineId,ReaderId,LibrarianId,Amount,IssuedAt,PaidAt,Reason")] Fine fine)
        {
            if (id != fine.FineId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    fine.IssuedAt = DateTime.SpecifyKind(fine.IssuedAt, DateTimeKind.Utc);

                    if (fine.PaidAt.HasValue)
                        fine.PaidAt = DateTime.SpecifyKind(fine.PaidAt.Value, DateTimeKind.Utc);

                    _context.Update(fine);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FineExists(fine.FineId))
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
            ViewData["LibrarianId"] = new SelectList(_context.Users, "UserId", "Email", fine.LibrarianId);
            ViewData["ReaderId"] = new SelectList(_context.Users, "UserId", "Email", fine.ReaderId);
            return View(fine);
        }
        
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fine = await _context.Fines
                .Include(f => f.Librarian)
                .Include(f => f.Reader)
                .FirstOrDefaultAsync(m => m.FineId == id);
            if (fine == null)
            {
                return NotFound();
            }

            return View(fine);
        }
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var fine = await _context.Fines.FindAsync(id);

            if (fine != null)
            {
                _context.Fines.Remove(fine);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Штраф удалён.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool FineExists(long id)
        {
            return _context.Fines.Any(e => e.FineId == id);
        }
    }
}
