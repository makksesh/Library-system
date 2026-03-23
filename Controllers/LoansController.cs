using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibApp.Data;
using LibApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace LibApp.Controllers
{
    [Authorize(Roles="Admin, Librarian")]
    public class LoansController : Controller
    {
        private readonly AppDbContext _context;

        public LoansController(AppDbContext context)
        {
            _context = context;
        }

        // GET
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Loans.Include(l => l.ExampleBook).Include(l => l.User);
            return View(await appDbContext.ToListAsync());
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans
                .Include(l => l.ExampleBook)
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.LoanId == id);
            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }

        #region Create

        public IActionResult Create()
        {
            ViewData["ExampleBookId"] = new SelectList(_context.ExampleBooks, "ExampleBookId", "ExampleBookId");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Email");
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LoanId,UserId,ExampleBookId,IssuedAt,DueDate,ExtensionsCount,ReturnedAt")] Loan loan)
        {
            if (ModelState.IsValid)
            {
                loan.IssuedAt = DateTime.SpecifyKind(loan.IssuedAt, DateTimeKind.Utc);
                loan.DueDate = DateTime.SpecifyKind(loan.DueDate, DateTimeKind.Utc);
                if (loan.ReturnedAt.HasValue)
                    loan.ReturnedAt = DateTime.SpecifyKind(loan.ReturnedAt.Value, DateTimeKind.Utc);

                _context.Add(loan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ExampleBookId"] = new SelectList(_context.ExampleBooks, "ExampleBookId", "ExampleBookId", loan.ExampleBookId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Email", loan.UserId);
            return View(loan);
        }

        #endregion

        #region Edit

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans.FindAsync(id);
            if (loan == null)
            {
                return NotFound();
            }
            ViewData["ExampleBookId"] = new SelectList(_context.ExampleBooks, "ExampleBookId", "ExampleBookId", loan.ExampleBookId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Email", loan.UserId);
            return View(loan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("LoanId,UserId,ExampleBookId,IssuedAt,DueDate,ExtensionsCount,ReturnedAt")] Loan loan)
        {
            if (id != loan.LoanId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    loan.IssuedAt = DateTime.SpecifyKind(loan.IssuedAt, DateTimeKind.Utc);
                    loan.DueDate = DateTime.SpecifyKind(loan.DueDate, DateTimeKind.Utc);
                    if (loan.ReturnedAt.HasValue)
                        loan.ReturnedAt = DateTime.SpecifyKind(loan.ReturnedAt.Value, DateTimeKind.Utc);

                    _context.Update(loan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoanExists(loan.LoanId))
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
            ViewData["ExampleBookId"] = new SelectList(_context.ExampleBooks, "ExampleBookId", "ExampleBookId", loan.ExampleBookId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Email", loan.UserId);
            return View(loan);
        }

        #endregion

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            var loan = await _context.Loans
                .Include(l => l.ExampleBook)
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.LoanId == id);

            if (loan == null) return NotFound();

            return View(loan);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var loan = await _context.Loans
                .Include(l => l.ExampleBook)
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.LoanId == id);

            if (loan == null)
                return RedirectToAction(nameof(Index));
            
            if (loan.ReturnedAt == null && loan.ExampleBook != null)
                loan.ExampleBook.Status = BookStatus.Available;

            _context.Loans.Remove(loan);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Выдача удалена.";
            return RedirectToAction(nameof(Index));
        }


        private bool LoanExists(long id)
        {
            return _context.Loans.Any(e => e.LoanId == id);
        }
    }
}
