using System.ComponentModel.DataAnnotations;

namespace SpaceMissions.WebAP.DTOs;

public class RocketDto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Название ракеты обязательно")]
    [StringLength(100, ErrorMessage = "Название ракеты не должно превышать 100 символов")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Статус активности обязателен")]
    public bool IsActive { get; set; }
}