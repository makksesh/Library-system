using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LibApp.Models;

public class ExampleBook
{
    [Key]
    [Display(Name = "Экземпляр книги")]
    public long ExampleBookId { get; set; }
    [Display(Name = "Издание")]
    public long VersionBookId { get; set; }
    
    [Display(Name = "Ячейка / Шифр полки")]
    [MaxLength(20)]
    public string? ShelfCode { get; set; } // "B-14", "A-03-2"

    [Display(Name = "Статус экземпляра")]
    public BookStatus Status { get; set; } = BookStatus.Available;

    [Display(Name = "Состояние книги")]
    public BookCondition Condition { get; set; } = BookCondition.Good;

    [ValidateNever]
    [Display(Name = "Издательство")]
    public VersionBook VersionBook { get; set; } = null!;
    
    public bool IsDeleted { get; set; } =  false;
}


public enum BookStatus
{
    [Display(Name = "В наличии")]       Available,
    [Display(Name = "У читателя")]         OnLoan,
    [Display(Name = "Забронирована")]  Reserved,
    [Display(Name = "В ремонте")]    Restoration,
    [Display(Name = "Списана")]        WriteOff,
    [Display(Name = "Утеряна")]        Lost
}

public enum BookCondition
{
    [Display(Name = "Новая")]                  New,
    [Display(Name = "Хорошее")]                Good,
    [Display(Name = "Удовлетворительное")]      Fair,
    [Display(Name = "Плохое")]                 Poor,
    [Display(Name = "Повреждена")]             Damaged
}