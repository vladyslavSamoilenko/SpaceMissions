using CsvHelper.Configuration.Attributes;

namespace SpaceMissions.WebAP.DTOs;

public class MissionCsvRecord
{
    public string Company { get; set; }
    public string Location { get; set; }
    public string Date { get; set; }    
    public string Time { get; set; }     
    public string Rocket { get; set; }
    public string Mission { get; set; }
    public string RocketStatus { get; set; }
    public string Price { get; set; }   
    public string MissionStatus { get; set; }
}