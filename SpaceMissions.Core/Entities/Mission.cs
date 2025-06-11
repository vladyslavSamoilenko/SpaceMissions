namespace SpaceMissions.Core.Entities;

public class Mission
{
    public int Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime LaunchDateTime { get; set; }
    public string MissionName { get; set; } = string.Empty; 
    public string MissionStatus { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    
    public int? RocketId { get; set; }
    public Rocket? Rocket { get; set; }
}