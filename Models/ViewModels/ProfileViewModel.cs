namespace LibApp.Models.ViewModels;

public class ProfileViewModel
{
    public User User { get; set; } = null!;

    // Книги, которые сейчас на руках (ReturnedAt == null, не бронь)
    public List<Loan> ActiveLoans { get; set; } = new();

    // Брони (ReturnedAt == null, DueDate скоро)
    public List<Loan> Reservations { get; set; } = new();

    // Завершённые выдачи
    public int TotalRead { get; set; }

    // Книги, которые нужно вернуть в ближайшие N дней
    public List<Loan> SoonDue { get; set; } = new();
}