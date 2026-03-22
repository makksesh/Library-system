using LibApp.Data;
using LibApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db) => _db = db;

    public async Task<IActionResult> BookStats(string? search)
    {

        var versions = await _db.VersionBooks
            .Include(v => v.Book).ThenInclude(b => b.Author)
            .Include(v => v.ExampleBooks)
            .ToListAsync();


        var allExampleIds = versions
            .SelectMany(v => v.ExampleBooks)
            .Select(eb => eb.ExampleBookId)
            .ToList();

        var activeExampleIds = await _db.Loans
            .Where(l => allExampleIds.Contains(l.ExampleBookId) && l.ReturnedAt == null)
            .Select(l => l.ExampleBookId)
            .ToHashSetAsync();


        var query = versions
            .GroupBy(v => v.Book)
            .Select(g => new AdminBookStatsItem
            {
                BookId    = g.Key.BookId,
                Title     = g.Key.Name,
                Author    = g.Key.Author.FullName,
                TotalCopies  = g.SelectMany(v => v.ExampleBooks).Count(),
                IssuedCount  = g.SelectMany(v => v.ExampleBooks)
                    .Count(eb => activeExampleIds.Contains(eb.ExampleBookId))
            });


        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Author.Contains(search, StringComparison.OrdinalIgnoreCase));

        ViewBag.Search = search;
        return View(query.OrderBy(x => x.Title).ToList());
    }
}