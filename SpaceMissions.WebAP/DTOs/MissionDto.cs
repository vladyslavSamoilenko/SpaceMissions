using System.ComponentModel.DataAnnotations;

namespace SpaceMissions.WebAP.DTOs;

public class MissionDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mission name is required")]
    [StringLength(200, ErrorMessage = "Mission name must not exceed 200 characters")]
    public string MissionName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Launch date and time are required")]
    public DateTime LaunchDateTime { get; set; }

    [Required(ErrorMessage = "Company is required")]
    [StringLength(100, ErrorMessage = "Company name must not exceed 100 characters")]
    public string Company { get; set; } = string.Empty;

    [Required(ErrorMessage = "Launch location is required")]
    [StringLength(200, ErrorMessage = "Launch location must not exceed 200 characters")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mission status is required")]
    [StringLength(50, ErrorMessage = "Mission status must not exceed 50 characters")]
    public string MissionStatus { get; set; } = string.Empty;

    [Range(0, 1000000, ErrorMessage = "Price must be between 0 and 1,000,000")]
    public decimal? Price { get; set; }

    public int? RocketId { get; set; }

    [Required(ErrorMessage = "Rocket name is required")]
    public string RocketName { get; set; } = string.Empty;
}