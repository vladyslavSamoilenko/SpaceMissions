namespace SpaceMissions.Core.Entities;

public class Mission
{
    public int Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime LaunchDateTime { get; set; }  
    public string RocketName { get; set; } = string.Empty; 
    public string MissionName { get; set; } = string.Empty;
    public string RocketStatus { get; set; } = string.Empty;
    public decimal? Price { get; set; }            
    public string MissionStatus { get; set; } = string.Empty;
    
    public int? RocketId { get; set; }
    public Rocket? Rocket { get; set; }
}