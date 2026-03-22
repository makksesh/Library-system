namespace LibApp.Models.ViewModels;

public class AdminBookStatsItem
{
    public long BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int TotalCopies { get; set; }   // все экземпляры
    public int IssuedCount { get; set; }   // активные выдачи (ReturnedAt == null)
    public int AvailableCount => TotalCopies - IssuedCount; // остаток
}