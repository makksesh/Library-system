using LibApp.Data;
using LibApp.Models;
using LibApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibApp.Controllers;

[Authorize(Roles = "Admin,Librarian")]
public class LibrarianController : Controller
{
    private readonly AppDbContext _db;

    public LibrarianController(AppDbContext db)
    {
        _db = db;
    }


    [HttpGet]
    public IActionResult Index() => View();

    // POST: поиск читателя по ID
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FindReader(long readerId)
    {
        var reader = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == readerId);

        if (reader == null)
        {
            TempData["Error"] = $"Читатель с ID {readerId} не найден.";
            return RedirectToAction(nameof(Index));
        }

        var allActiveLoans = await _db.Loans
            .Where(l => l.UserId == readerId && l.ReturnedAt == null)
            .Include(l => l.ExampleBook)
                .ThenInclude(eb => eb.VersionBook)
                    .ThenInclude(vb => vb.Book)
                        .ThenInclude(b => b.Author)
            .OrderBy(l => l.DueDate)
            .ToListAsync();

        var reservations = allActiveLoans
            .Where(l => (l.DueDate - l.IssuedAt).TotalDays <= 3)
            .ToList();

        var activeLoans = allActiveLoans
            .Where(l => (l.DueDate - l.IssuedAt).TotalDays > 3)
            .ToList();

        var fines = await _db.Fines
            .Where(f => f.ReaderId == readerId)
            .OrderBy(f => f.PaidAt.HasValue) // неоплаченные первыми
            .ThenByDescending(f => f.IssuedAt)
            .ToListAsync();

        var vm = new ReaderDetailsViewModel
        {
            Reader = reader,
            Reservations = reservations,
            ActiveLoans = activeLoans,
            Fines = fines
        };

        return View("ReaderDetails", vm);
    }

    // POST: выдать забронированную книгу
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssueReserved(long loanId, DateTime dueDate, long readerId)
    {
        var loan = await _db.Loans.FindAsync(loanId);
        if (loan == null)
        {
            TempData["Error"] = "Резервация не найдена.";
            return RedirectToAction(nameof(FindReaderGet), new { readerId });
        }

        var minDueDate = DateTime.UtcNow.AddDays(10);
        if (DateTime.SpecifyKind(dueDate, DateTimeKind.Utc) < minDueDate)
        {
            TempData["Error"] = "Дата возврата должна быть не менее чем через 10 дней.";
            return RedirectToAction(nameof(FindReaderGet), new { readerId });
        }

        loan.DueDate = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc);
        loan.IssuedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Книга успешно выдана.";
        return RedirectToAction(nameof(FindReaderGet), new { readerId });
    }

    // GET-версия поиска для редиректа после действий
    [HttpGet]
    public async Task<IActionResult> FindReaderGet(long readerId)
    {
        // Симулируем POST через GET для редиректа
        return await FindReader(readerId);
    }

    // POST: оплатить штраф
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayFine(long fineId, long readerId)
    {
        var fine = await _db.Fines.FindAsync(fineId);
        if (fine != null)
        {
            fine.PaidAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Штраф отмечен как оплаченный.";
        }

        return RedirectToAction(nameof(FindReaderGet), new { readerId });
    }

    // POST: выдать книгу напрямую по ID экземпляра
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssueBook(long exampleBookId, DateTime dueDate, long readerId)
    {
        var minDueDate = DateTime.UtcNow.AddDays(10);
        if (DateTime.SpecifyKind(dueDate, DateTimeKind.Utc) < minDueDate)
        {
            TempData["Error"] = "Дата возврата должна быть не менее чем через 10 дней.";
            return RedirectToAction(nameof(FindReaderGet), new { readerId });
        }

        var example = await _db.ExampleBooks.FindAsync(exampleBookId);
        if (example == null)
        {
            TempData["Error"] = $"Экземпляр с ID {exampleBookId} не найден.";
            return RedirectToAction(nameof(FindReaderGet), new { readerId });
        }

        var alreadyOnLoan = await _db.Loans
            .AnyAsync(l => l.ExampleBookId == exampleBookId && l.ReturnedAt == null);
        if (alreadyOnLoan)
        {
            TempData["Error"] = "Этот экземпляр уже выдан.";
            return RedirectToAction(nameof(FindReaderGet), new { readerId });
        }

        var loan = new Loan
        {
            UserId = readerId,
            ExampleBookId = exampleBookId,
            IssuedAt = DateTime.UtcNow,
            DueDate = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc),
            ExtensionsCount = 0,
            ReturnedAt = null
        };

        _db.Loans.Add(loan);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Книга успешно выдана.";
        return RedirectToAction(nameof(FindReaderGet), new { readerId });
    }
    
    // GET: страница каталога для выдачи книги читателю
    [HttpGet]
    public async Task<IActionResult> IssueBookCatalog(long readerId, string? search, string? category)
    {
        var query = _db.VersionBooks
            .Include(v => v.Book).ThenInclude(b => b.Author)
            .Include(v => v.Book).ThenInclude(b => b.Category)
            .Include(v => v.ExampleBooks)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.Book.Name.Contains(search)
                                  || v.Book.Author.FullName.Contains(search));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(v => v.Book.Category.Name == category);

        var versions = await query.ToListAsync();

        var allExampleIds = versions
            .SelectMany(v => v.ExampleBooks)
            .Select(eb => eb.ExampleBookId)
            .ToList();

        var busyIds = await _db.Loans
            .Where(l => allExampleIds.Contains(l.ExampleBookId) && l.ReturnedAt == null)
            .Select(l => l.ExampleBookId)
            .ToListAsync();

        var model = versions.Select(v =>
        {
            var freeExample = v.ExampleBooks
                .FirstOrDefault(eb => !busyIds.Contains(eb.ExampleBookId));

            return new IssueBookCatalogItem
            {
                VersionBookId    = v.VersionBookId,
                Title            = v.Book.Name,
                Author           = v.Book.Author.FullName,
                Category         = v.Book.Category.Name,
                Year             = v.CreateAt.Year,
                TotalExamples    = v.ExampleBooks.Count,
                AvailableExamples = v.ExampleBooks.Count(eb => !busyIds.Contains(eb.ExampleBookId)),
                FreeExampleBookId = freeExample?.ExampleBookId
            };
        }).ToList();

        ViewBag.ReaderId         = readerId;
        ViewBag.ReaderName       = (await _db.Users.FindAsync(readerId))?.FullName ?? readerId.ToString();
        ViewBag.Search           = search;
        ViewBag.SelectedCategory = category;
        ViewBag.Categories       = await _db.Categories.Select(c => c.Name).ToListAsync();

        return View(model);
    }

}