using System.Security.Claims;
using LibApp.Data;
using LibApp.Models;
using LibApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibApp.Controllers;

[Authorize(Roles = "Admin,Librarian")]
public class ReturnBookController : Controller
{
    private readonly AppDbContext _context;

    public ReturnBookController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /ReturnBook/Accept/{loanId}
    [HttpGet]
    public async Task<IActionResult> Accept(long loanId)
    {
        var loan = await _context.Loans
            .Include(l => l.User)
            .Include(l => l.ExampleBook)
                .ThenInclude(eb => eb.VersionBook)
                    .ThenInclude(vb => vb.Book)
                        .ThenInclude(b => b.Author)
            .Include(l => l.ExampleBook)
                .ThenInclude(eb => eb.VersionBook)
                    .ThenInclude(vb => vb.Publisher)
            .FirstOrDefaultAsync(l => l.LoanId == loanId && l.ReturnedAt == null);

        if (loan == null)
            return NotFound();

        var now = DateTime.UtcNow;
        var isOverdue = now > loan.DueDate;
        var overdueDays = isOverdue ? (int)Math.Ceiling((now - loan.DueDate).TotalDays) : 0;

        var vm = new ReturnBookViewModel
        {
            Loan = loan,
            IsOverdue = isOverdue,
            OverdueDays = overdueDays
        };

        return View(vm);
    }

    // POST: /ReturnBook/ReturnWithoutFine
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnWithoutFine(long loanId)
    {
        var loan = await _context.Loans
            .FirstOrDefaultAsync(l => l.LoanId == loanId && l.ReturnedAt == null);

        if (loan == null)
            return NotFound();

        loan.ReturnedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Книга успешно принята. Штраф не начислен.";
        return RedirectToAction("Index", "Loans");
    }

    // POST: /ReturnBook/ReturnWithFine
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnWithFine(long loanId, decimal pricePerDay)
    {
        var loan = await _context.Loans
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.LoanId == loanId && l.ReturnedAt == null);

        if (loan == null)
            return NotFound();

        var now = DateTime.UtcNow;
        var overdueDays = (int)Math.Ceiling((now - loan.DueDate).TotalDays);

        if (overdueDays <= 0 || pricePerDay <= 0)
        {
            TempData["Error"] = "Некорректные данные для штрафа.";
            return RedirectToAction("Accept", new { loanId });
        }

        var librarianIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(librarianIdStr, out var librarianId))
            return Unauthorized();

        var fine = new Fine
        {
            ReaderId = loan.UserId,
            LibrarianId = librarianId,
            Amount = pricePerDay * overdueDays,
            IssuedAt = DateTime.UtcNow,
            Reason = $"Просрочка {overdueDays} дн. × {pricePerDay:F2} ₽/день"
        };

        _context.Fines.Add(fine);

        loan.ReturnedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Штраф {fine.Amount:F2} ₽ начислен. Книга принята.";
        return RedirectToAction("Index", "Loans");
    }
}