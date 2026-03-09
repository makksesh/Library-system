// Controllers/ProfileController.cs
using LibApp.Data;
using LibApp.Models;
using LibApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibApp.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly AppDbContext _context;
    private const int SoonDaysTreshold = 3; // предупреждать за 3 дня до возврата

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // ищем текущего пользователя по Login из Claims
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Login == User.Identity!.Name);

        if (user == null) return RedirectToAction("Login", "Account");

        var now = DateTime.UtcNow;

        // Все активные записи Loan для пользователя
        var allActiveLoans = await _context.Loans
            .Where(l => l.UserId == user.UserId && l.ReturnedAt == null)
            .Include(l => l.ExampleBook)
                .ThenInclude(eb => eb.VersionBook)
                    .ThenInclude(vb => vb.Book)
                        .ThenInclude(b => b.Author)
            .Include(l => l.ExampleBook)
                .ThenInclude(eb => eb.VersionBook)
                    .ThenInclude(vb => vb.Book)
                        .ThenInclude(b => b.Category)
            .OrderBy(l => l.DueDate)
            .ToListAsync();

        // Разделяем: бронь = DueDate <= 3 дней с момента IssuedAt (короткий срок)
        // Реальная выдача = DueDate > 3 дней с IssuedAt
        // (логика зависит от твоей договорённости — здесь по сроку)
        var reservations = allActiveLoans
            .Where(l => (l.DueDate - l.IssuedAt).TotalDays <= 3)
            .ToList();

        var activeLoans = allActiveLoans
            .Where(l => (l.DueDate - l.IssuedAt).TotalDays > 3)
            .ToList();

        // Напоминания: книги, которые нужно вернуть в течение SoonDaysTreshold дней
        var soonDue = activeLoans
            .Where(l => (l.DueDate - now).TotalDays <= SoonDaysTreshold)
            .ToList();

        // Всего прочитано (возвращённые)
        var totalRead = await _context.Loans
            .CountAsync(l => l.UserId == user.UserId && l.ReturnedAt != null);

        var vm = new ProfileViewModel
        {
            User = user,
            ActiveLoans = activeLoans,
            Reservations = reservations,
            SoonDue = soonDue,
            TotalRead = totalRead
        };

        return View(vm);
    }

    // Редактирование только своего профиля
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Login == User.Identity!.Name);
        if (user == null) return NotFound();
        return View(user);
    }

    // POST: /Profile/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("UserId,FullName,Email,PhoneNumber")] User edited)
    {
        // Убираем валидацию полей, которые мы не редактируем
        ModelState.Remove("Login");
        ModelState.Remove("HashPass");
        ModelState.Remove("Role");
        ModelState.Remove("RoleId");

        if (!ModelState.IsValid)
            return View(edited);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Login == User.Identity!.Name);

        if (user == null) return NotFound();

        // Проверяем уникальность Email (если изменился)
        var emailTaken = await _context.Users
            .AnyAsync(u => u.Email == edited.Email && u.UserId != user.UserId);

        if (emailTaken)
        {
            ModelState.AddModelError("Email", "Этот email уже используется другим пользователем.");
            return View(edited);
        }

        // Обновляем только безопасные поля
        user.FullName    = edited.FullName;
        user.Email       = edited.Email;
        user.PhoneNumber = edited.PhoneNumber;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Профиль успешно обновлён.";
        return RedirectToAction(nameof(Index));
    }
    
    // POST: /Profile/Extend/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Extend(long loanId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Login == User.Identity!.Name);

        if (user == null) return NotFound();

        var loan = await _context.Loans
            .FirstOrDefaultAsync(l => l.LoanId == loanId && l.UserId == user.UserId);

        if (loan == null) return NotFound();

        // Проверка: не просрочена
        if (loan.DueDate < DateTime.UtcNow)
        {
            TempData["Error"] = "Нельзя продлить просроченную книгу. Обратитесь к библиотекарю.";
            return RedirectToAction(nameof(Index));
        }

        // Проверка: не больше 1 самостоятельного продления
        if (loan.ExtensionsCount >= 1)
        {
            TempData["Error"] = "Самостоятельное продление уже использовано. Обратитесь к библиотекарю.";
            return RedirectToAction(nameof(Index));
        }

        loan.DueDate = loan.DueDate.AddDays(14);
        loan.ExtensionsCount += 1;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Срок возврата продлён на 14 дней.";
        return RedirectToAction(nameof(Index));
    }

}
