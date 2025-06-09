namespace SpaceMissions.Core.Entities;

public class Rocket
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public ICollection<Mission>? Missions { get; set; }
}