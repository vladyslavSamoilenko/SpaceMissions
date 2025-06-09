namespace SpaceMissions.WebAP.DTOs;

public class MissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime LaunchDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RocketId { get; set; }
}