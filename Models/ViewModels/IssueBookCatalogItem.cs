namespace LibApp.Models.ViewModels;

public class IssueBookCatalogItem
{
    public long VersionBookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Year { get; set; }
    public int TotalExamples { get; set; }
    public int AvailableExamples { get; set; }
    
    public long? FreeExampleBookId { get; set; }
}