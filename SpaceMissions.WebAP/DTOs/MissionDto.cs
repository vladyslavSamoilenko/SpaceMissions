using System.ComponentModel.DataAnnotations;

namespace SpaceMissions.WebAP.DTOs;

public class MissionDto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Название миссии обязательно")]
    [StringLength(200, ErrorMessage = "Название миссии не должно превышать 200 символов")]
    public string MissionName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Дата и время запуска обязательны")]
    public DateTime LaunchDateTime { get; set; }
    
    [Required(ErrorMessage = "Компания обязательна")]
    [StringLength(100, ErrorMessage = "Название компании не должно превышать 100 символов")]
    public string Company { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Место запуска обязательно")]
    [StringLength(200, ErrorMessage = "Место запуска не должно превышать 200 символов")]
    public string Location { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Статус миссии обязателен")]
    [StringLength(50, ErrorMessage = "Статус миссии не должен превышать 50 символов")]
    public string MissionStatus { get; set; } = string.Empty;
    
    [Range(0, 1000000, ErrorMessage = "Цена должна быть между 0 и 1 000 000")]
    public decimal? Price { get; set; }
    
    public int? RocketId { get; set; }
    
    [Required(ErrorMessage = "Название ракеты обязательно")]
    public string RocketName { get; set; } = string.Empty;
}