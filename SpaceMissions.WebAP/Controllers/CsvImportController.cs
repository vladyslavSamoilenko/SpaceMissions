using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using SpaceMissions.Core.Entities;
using SpaceMissions.Infrastructure.Data;
using SpaceMissions.WebAP.DTOs;
using System.Globalization;

namespace SpaceMissions.WebAP.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CsvImportController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly SpaceMissionsDbContext _context;

    public CsvImportController(IWebHostEnvironment env, SpaceMissionsDbContext context)
    {
        _env = env;
        _context = context;
    }

    [HttpPost("import")]
    public IActionResult Import()
    {
        var csvPath = Path.Combine(_env.ContentRootPath, "Data", "space_missions.csv");

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        var records = csv.GetRecords<MissionCsvRecord>().ToList();

        foreach (var rec in records)
        {
            if (!DateTime.TryParse($"{rec.Date}T{rec.Time}Z", out var launchDateTime))
                continue;

            var rocket = _context.Rockets.FirstOrDefault(r => r.Name == rec.Rocket);
            if (rocket == null)
            {
                rocket = new Rocket
                {
                    Name = rec.Rocket,
                    IsActive = rec.RocketStatus.Equals("Active", StringComparison.OrdinalIgnoreCase)
                };
                _context.Rockets.Add(rocket);
                _context.SaveChanges(); // Чтобы получить Id ракеты
            }

            var mission = new Mission
            {
                Company = rec.Company,
                Location = rec.Location,
                LaunchDateTime = DateTime.SpecifyKind(launchDateTime, DateTimeKind.Utc),
                RocketName = rec.Rocket,
                MissionName = rec.Mission,
                RocketStatus = rec.RocketStatus,
                MissionStatus = rec.MissionStatus,
                Price = decimal.TryParse(rec.Price?.Replace("$", "").Replace(",", ""), out var price) ? price : null,
                RocketId = rocket.Id
            };

            _context.Missions.Add(mission);
        }

        _context.SaveChanges();

        return Ok(new { Count = records.Count });
    }

}