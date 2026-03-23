using LibApp.Data;
using LibApp.Models.ViewModels;
using LibApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;


public class ExampleBooksController : Controller
{
    private readonly AppDbContext _context;

    public ExampleBooksController(AppDbContext context)
    {
        _context = context;
    }
    
    [Authorize(Roles = "Admin,Librarian")]
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? category, string? status, bool showDeleted = false)
    {
        var query = _context.ExampleBooks
            .Include(eb => eb.VersionBook)
                .ThenInclude(v => v.Book)
                    .ThenInclude(b => b.Author)
            .Include(eb => eb.VersionBook)
                .ThenInclude(v => v.Book)
                    .ThenInclude(b => b.Category)
            .Where(eb => eb.IsDeleted == showDeleted)
            .AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (long.TryParse(search.Trim(), out var id))
                query = query.Where(eb => eb.ExampleBookId == id);
            else
                query = query.Where(eb =>
                    eb.VersionBook.Book.Name.Contains(search) ||
                    eb.VersionBook.Book.Author.FullName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(eb => eb.VersionBook.Book.Category.Name == category);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookStatus>(status, out var parsedStatus))
            query = query.Where(eb => eb.Status == parsedStatus);

        var examples = await query.ToListAsync();

        var exampleIds = examples.Select(eb => eb.ExampleBookId).ToList();
        var busyIds = await _context.Loans
            .Where(l => exampleIds.Contains(l.ExampleBookId) && l.ReturnedAt == null)
            .Select(l => l.ExampleBookId)
            .ToListAsync();

        var model = examples.Select(eb => new ExampleBookCatalogItem
        {
            ExampleBookId = eb.ExampleBookId,
            Title         = eb.VersionBook.Book.Name,
            Author        = eb.VersionBook.Book.Author.FullName,
            Category      = eb.VersionBook.Book.Category.Name,
            ShelfCode     = eb.ShelfCode,
            Status        = eb.Status,
            Condition     = eb.Condition,
            IsOnLoan      = busyIds.Contains(eb.ExampleBookId),
            IsDeleted     = eb.IsDeleted 
        }).ToList();

        ViewBag.Search           = search;
        ViewBag.SelectedCategory = category;
        ViewBag.SelectedStatus   = status;
        ViewBag.Categories       = await _context.Categories.Select(c => c.Name).ToListAsync();
        ViewBag.Statuses         = Enum.GetValues<BookStatus>()
            .Select(s => new SelectListItem(s.ToString(), s.ToString()))
            .ToList();

        return View(model);
    }
    
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null) return NotFound();

        var eb = await _context.ExampleBooks
            .Include(e => e.VersionBook).ThenInclude(v => v.Book).ThenInclude(b => b.Author)
            .Include(e => e.VersionBook).ThenInclude(v => v.Book).ThenInclude(b => b.Category)
            .FirstOrDefaultAsync(e => e.ExampleBookId == id);

        if (eb == null) return NotFound();
        return View(eb);
    }

    #region Create

    public IActionResult Create()
    {
        ViewData["VersionBookId"] = new SelectList(
            _context.VersionBooks
                .Include(v => v.Book)
                .Select(v => new { v.VersionBookId, Display = v.Book.Name + " (" + v.Name + ")" }),
            "VersionBookId", "Display");
        return View();
    }
    
    [Authorize(Roles = "Admin,Librarian")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("VersionBookId,ShelfCode,Status,Condition")] ExampleBook eb)
    {
        if (ModelState.IsValid)
        {
            _context.Add(eb);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Экземпляр успешно добавлен.";
            return RedirectToAction(nameof(Index));
        }
        ViewData["VersionBookId"] = new SelectList(
            _context.VersionBooks.Include(v => v.Book)
                .Select(v => new { v.VersionBookId, Display = v.Book.Name + " (" + v.Name + ")" }),
            "VersionBookId", "Display", eb.VersionBookId);
        return View(eb);
    }

    #endregion

    #region Edit

    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null) return NotFound();
        var eb = await _context.ExampleBooks.FindAsync(id);
        if (eb == null) return NotFound();

        ViewData["VersionBookId"] = new SelectList(
            _context.VersionBooks.Include(v => v.Book)
                .Select(v => new { v.VersionBookId, Display = v.Book.Name + " (" + v.Name + ")" }),
            "VersionBookId", "Display", eb.VersionBookId);
        return View(eb);
    }
    
    [Authorize(Roles = "Admin,Librarian")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id,
        [Bind("ExampleBookId,VersionBookId,ShelfCode,Status,Condition")] ExampleBook eb)
    {
        if (id != eb.ExampleBookId) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(eb);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Экземпляр обновлён.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ExampleBooks.Any(e => e.ExampleBookId == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        ViewData["VersionBookId"] = new SelectList(
            _context.VersionBooks.Include(v => v.Book)
                .Select(v => new { v.VersionBookId, Display = v.Book.Name + " (" + v.Name + ")" }),
            "VersionBookId", "Display", eb.VersionBookId);
        return View(eb);
    }
    
    [Authorize(Roles = "Admin,Librarian")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(long id)
    {
        var eb = await _context.ExampleBooks.FindAsync(id);
        if (eb != null)
        {
            eb.IsDeleted = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Экземпляр #{id} восстановлен.";
        }
        return RedirectToAction(nameof(Index), new { showDeleted = true });
    }

    #endregion
    
    #region Delete

    public async Task<IActionResult> Delete(long? id)
    {
        if (id == null) return NotFound();
        var eb = await _context.ExampleBooks
            .Include(e => e.VersionBook).ThenInclude(v => v.Book).ThenInclude(b => b.Author)
            .FirstOrDefaultAsync(e => e.ExampleBookId == id);
        if (eb == null) return NotFound();

        var hasLoans = await _context.Loans.AnyAsync(l => l.ExampleBookId == id && l.ReturnedAt == null);
        if (hasLoans)
            ViewBag.ErrorMessage = "Нельзя удалить экземпляр: по нему есть выдачи.";

        return View(eb);
    }
    
    [Authorize(Roles = "Admin,Librarian")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var hasActiveLoans = await _context.Loans
            .AnyAsync(l => l.ExampleBookId == id && l.ReturnedAt == null);
        if (hasActiveLoans)
        {
            TempData["Error"] = "Нельзя удалить экземпляр: по нему есть активные выдачи.";
            return RedirectToAction(nameof(Index));
        }

        var eb = await _context.ExampleBooks.FindAsync(id);
        if (eb != null)
        {
            eb.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Экземпляр помечен как удалённый.";
        }
        return RedirectToAction(nameof(Index));
    }

    #endregion


    #region Catalog

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Catalog(string? search, string? category)
    {
        var query = _context.VersionBooks
            .Include(v => v.Book)
            .ThenInclude(b => b.Author)
            .Include(v => v.Book)
            .ThenInclude(b => b.Category)
            .Include(v => v.ExampleBooks)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(v =>
                v.Book.Name.Contains(search) ||
                v.Book.Author.FullName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(v => v.Book.Category.Name == category);

        var versions = await query.ToListAsync();

        var allExampleIds = versions
            .SelectMany(v => v.ExampleBooks)
            .Select(eb => eb.ExampleBookId)
            .ToList();

        var busyIds = await _context.Loans
            .Where(l => allExampleIds.Contains(l.ExampleBookId) && l.ReturnedAt == null)
            .Select(l => l.ExampleBookId)
            .ToListAsync();

        var model = versions.Select(v =>
        {
            var available = v.ExampleBooks.Count(eb => !busyIds.Contains(eb.ExampleBookId));

            return new VersionCatalogItem
            {
                VersionBookId     = v.VersionBookId,
                Title             = v.Book.Name,
                Author            = v.Book.Author.FullName,
                Category          = v.Book.Category.Name,
                Year              = v.CreateAt.Year,
                TotalExamples     = v.ExampleBooks.Count,
                AvailableExamples = available
            };
        }).ToList();

        ViewBag.Search           = search;
        ViewBag.SelectedCategory = category;
        ViewBag.Categories       = await _context.Categories
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

    #endregion
}