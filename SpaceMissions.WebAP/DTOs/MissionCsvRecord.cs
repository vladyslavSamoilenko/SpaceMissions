using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SpaceMissions.WebAP.DTOs;

public class MissionCsvRecord
{
    [Name("Company")]
    [Required]
    public string Company { get; set; }

    [Name("Location")]
    [Required]
    public string Location { get; set; }

    [Name("Date")]
    [Required]
    public string Date { get; set; }

    [Name("Time")]
    [Optional]
    public string Time { get; set; }

    [Name("Rocket")]
    [Required]
    public string Rocket { get; set; }

    [Name("Mission")]
    [Required]
    public string Mission { get; set; }

    [Name("RocketStatus")]
    public string RocketStatus { get; set; }

    [Name("Price")]
    [Optional]
    public string Price { get; set; }

    [Name("MissionStatus")]
    public string MissionStatus { get; set; }
}