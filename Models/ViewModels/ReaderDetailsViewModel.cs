namespace LibApp.Models.ViewModels;

public class ReaderDetailsViewModel
{
    public User Reader { get; set; } = null!;
    public List<Loan> Reservations { get; set; } = new(); // DueDate - IssuedAt <= 3 дня
    public List<Loan> ActiveLoans { get; set; } = new();
    public List<Fine> Fines { get; set; } = new();
}