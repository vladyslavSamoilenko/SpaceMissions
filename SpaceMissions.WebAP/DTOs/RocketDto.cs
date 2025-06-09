namespace SpaceMissions.WebAP.DTOs;

public class RocketDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal? Price { get; set; }
}