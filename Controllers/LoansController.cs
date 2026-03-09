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

        // GET: Loans
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Loans.Include(l => l.ExampleBook).Include(l => l.User);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Loans/Details/5
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

        // GET: Loans/Create
        public IActionResult Create()
        {
            ViewData["ExampleBookId"] = new SelectList(_context.ExampleBooks, "ExampleBookId", "ExampleBookId");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Email");
            return View();
        }

        // POST: Loans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

        // GET: Loans/Edit/5
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

        // POST: Loans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

        // GET: Loans/Delete/5
        public async Task<IActionResult> Delete(long? id)
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

        // POST: Loans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var loan = await _context.Loans
                .Include(l => l.ExampleBook)
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.LoanId == id);

            if (loan.ExampleBook != null || loan.User != null)
            {
                ViewBag.ErrorMessage = "У этой выдчи есть экземпляр книги или пользователь";
                return View("Delete", loan);
            }
            if (loan != null)
            {
                _context.Loans.Remove(loan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LoanExists(long id)
        {
            return _context.Loans.Any(e => e.LoanId == id);
        }
    }
}
