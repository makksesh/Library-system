namespace LibApp.Models.ViewModels;

public class ExampleBookCatalogItem
{
    public long ExampleBookId   { get; set; }
    public string Title         { get; set; } = string.Empty;
    public string Author        { get; set; } = string.Empty;
    public string Category      { get; set; } = string.Empty;
    public string? ShelfCode    { get; set; }
    public BookStatus Status    { get; set; }
    public BookCondition Condition { get; set; }
    public bool IsOnLoan        { get; set; }  // есть активная выдача
    public bool IsDeleted       { get; set; }  // мягкое удаление
}