namespace LibApp.Models.ViewModels;

public class ReturnBookViewModel
{
    public Loan Loan { get; set; } = null!;
    public bool IsOverdue { get; set; }
    public int OverdueDays { get; set; }

    // Для формы штрафа
    public decimal PricePerDay { get; set; }
    public decimal TotalFine => PricePerDay * OverdueDays;
}