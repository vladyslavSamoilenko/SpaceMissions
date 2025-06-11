namespace SpaceMissions.WebAP.DTOs;

public class MissionListItemDto
{
    public int Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string MissionName { get; set; } = string.Empty;
    public string? RocketName { get; set; }
    public DateTime LaunchDateTime { get; set; }
    public string MissionStatus { get; set; } = string.Empty;
}