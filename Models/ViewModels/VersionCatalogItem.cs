namespace LibApp.Models.ViewModels;

public class VersionCatalogItem
{
    public long VersionBookId { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public int Year { get; set; }
    public int TotalExamples { get; set; }
    public int AvailableExamples { get; set; }
    public bool CanReserve => AvailableExamples > 0;
}