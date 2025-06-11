namespace SpaceMissions.WebAP.DTOs;

public class MissionFilterDto
{
    public string? Company { get; set; }
    public string? MissionStatus { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SortBy { get; set; } 
    public bool SortDescending { get; set; } = false;
}