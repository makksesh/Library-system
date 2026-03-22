using LibApp.Data;
using LibApp.Models.ViewModels;
using LibApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

public class ExampleBooksController : Controller
{
    private readonly AppDbContext _context;

    public ExampleBooksController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> Catalog(string? search, string? category)
    {
        var query = _context.VersionBooks
            .Include(v => v.Book)
            .ThenInclude(b => b.Author)
            .Include(v => v.Book.Category)
            .Include(v => v.ExampleBooks)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(v =>
                v.Book.Name.Contains(search) ||
                v.Book.Author.FullName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(v => v.Book.Category.Name == category);
        }

        var versions = await query
            .Include(v => v.ExampleBooks)
            .ToListAsync();
        
        var versionIds = versions.SelectMany(v => v.ExampleBooks)
                                 .Select(eb => eb.ExampleBookId)
                                 .Distinct()
                                 .ToList();

        var loans = await _context.Loans
            .Where(l => versionIds.Contains(l.ExampleBookId) &&
                        l.ReturnedAt == null)
            .ToListAsync();

        var model = versions.Select(v =>
        {
            var exampleIds = v.ExampleBooks.Select(eb => eb.ExampleBookId).ToList();
            var activeLoansForVersion = loans.Where(l => exampleIds.Contains(l.ExampleBookId)).ToList();

            var total = exampleIds.Count;
            var busy = activeLoansForVersion.Count;
            var available = total - busy;

            return new VersionCatalogItem
            {
                VersionBookId = v.VersionBookId,
                Title = v.Book.Name,
                Author = v.Book.Author.FullName,
                Category = v.Book.Category.Name,
                Year = v.CreateAt.Year,
                TotalExamples = total,
                AvailableExamples = available
            };
        }).ToList();

        ViewBag.Search = search;
        ViewBag.SelectedCategory = category;
        ViewBag.Categories = await _context.Categories
            .Select(c => c.Name)
            .ToListAsync();

        return View(model);
    }
    
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(long versionBookId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Login == User.Identity!.Name);

        if (user == null)
            return RedirectToAction("Login", "Account");


        var hasUnpaidFines = await _context.Fines
            .AnyAsync(f => f.ReaderId == user.UserId && f.PaidAt == null);

        if (hasUnpaidFines)
        {
            TempData["Error"] = "Бронирование недоступно: у вас есть неоплаченные штрафы.";
            return RedirectToAction(nameof(Catalog));
        }


        // Бронь = активный заём с DueDate - IssuedAt <= 3 дней
        var activeReservationsCount = await _context.Loans
            .CountAsync(l =>
                l.UserId == user.UserId &&
                l.ReturnedAt == null &&
                (l.DueDate - l.IssuedAt).TotalDays <= 3);

        if (activeReservationsCount >= 3)
        {
            TempData["Error"] = "Бронирование недоступно: нельзя иметь более 3 активных бронирований.";
            return RedirectToAction(nameof(Catalog));
        }


        var freeExample = await _context.ExampleBooks
            .Where(eb => eb.VersionBookId == versionBookId)
            .Where(eb => !_context.Loans
                .Any(l => l.ExampleBookId == eb.ExampleBookId && l.ReturnedAt == null))
            .FirstOrDefaultAsync();

        if (freeExample == null)
        {
            TempData["Error"] = "Нет доступных экземпляров.";
            return RedirectToAction(nameof(Catalog), new { id = versionBookId });
        }

        var now = DateTime.UtcNow;
        var loan = new Loan
        {
            UserId = user.UserId,
            ExampleBookId = freeExample.ExampleBookId,
            IssuedAt = now,
            DueDate = now.AddDays(3), 
            ExtensionsCount = 0,
            ReturnedAt = null
        };

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Книга успешно забронирована. Срок — 3 дня.";
        return RedirectToAction(nameof(Catalog));
    }

}
