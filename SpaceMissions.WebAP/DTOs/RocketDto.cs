using System.ComponentModel.DataAnnotations;

namespace SpaceMissions.WebAP.DTOs;

public class RocketDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Rocket name is required")]
    [StringLength(100, ErrorMessage = "Rocket name must not exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Active status is required")]
    public bool IsActive { get; set; }
}