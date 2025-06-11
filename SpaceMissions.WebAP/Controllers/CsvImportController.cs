using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpaceMissions.Core.Entities;
using SpaceMissions.Infrastructure.Data;
using SpaceMissions.WebAP.DTOs;
using System.Globalization;

namespace SpaceMissions.WebAP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CsvImportController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly SpaceMissionsDbContext _context;
        private readonly ILogger<CsvImportController> _logger;

        public CsvImportController(
            IWebHostEnvironment env,
            SpaceMissionsDbContext context,
            ILogger<CsvImportController> logger)
        {
            _env = env;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Импортирует миссии и ракеты из CSV-файла.
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import()
        {
            var csvPath = Path.Combine(_env.ContentRootPath, "Data", "space_missions.csv");
            if (!System.IO.File.Exists(csvPath))
            {
                _logger.LogError("CSV file not found at {Path}", csvPath);
                return NotFound(new { Message = "CSV file not found" });
            }

            // Читаем все записи из CSV
            List<MissionCsvRecord> records;
            using (var reader = new StreamReader(csvPath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            }))
            {
                records = csv.GetRecords<MissionCsvRecord>().ToList();
            }
            _logger.LogInformation("Loaded {Count} CSV records", records.Count);

            // Словарь для ускоренного поиска и хранения новых ракет
            var rocketDict = new Dictionary<string, Rocket>(StringComparer.OrdinalIgnoreCase);
            var missions = new List<Mission>();

            foreach (var rec in records)
            {
                if (string.IsNullOrWhiteSpace(rec.Rocket) || string.IsNullOrWhiteSpace(rec.Mission))
                {
                    _logger.LogWarning("Skipping invalid record: {@Record}", rec);
                    continue;
                }

                // Получаем существующую ракету или создаём новую
                if (!rocketDict.TryGetValue(rec.Rocket, out var rocket))
                {
                    rocket = await _context.Rockets
                        .FirstOrDefaultAsync(r => r.Name == rec.Rocket)
                        ?? new Rocket
                        {
                            Name = rec.Rocket,
                            IsActive = rec.RocketStatus?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true
                        };
                    rocketDict[rocket.Name] = rocket;
                }

                // Парсим дату и время запуска в UTC
                if (!TryParseDateTime(rec.Date, rec.Time, out var launchTimeUtc))
                {
                    _logger.LogWarning("Invalid date/time for mission '{Mission}'", rec.Mission);
                    continue;
                }

                // Создаём объект Mission с навигационной ссылкой на Rocket
                missions.Add(new Mission
                {
                    Company = rec.Company,
                    Location = rec.Location,
                    LaunchDateTime = launchTimeUtc,
                    MissionName = rec.Mission,
                    MissionStatus = rec.MissionStatus,
                    Price = TryParsePrice(rec.Price),
                    Rocket = rocket
                });
            }

            // Сохраняем новые ракеты и миссии в одной транзакции
            var newRockets = rocketDict.Values.Where(r => r.Id == 0).ToList();
            if (newRockets.Any())
                await _context.Rockets.AddRangeAsync(newRockets);

            await _context.Missions.AddRangeAsync(missions);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Imported rockets: {Count}, missions: {Count}", newRockets.Count, missions.Count);
            return Ok(new { RocketsAdded = newRockets.Count, MissionsAdded = missions.Count });
        }

        private static bool TryParseDateTime(string date, string time, out DateTime value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(date))
                return false;

            DateTime parsed;
            if (string.IsNullOrWhiteSpace(time))
            {
                if (!DateTime.TryParse(date, CultureInfo.InvariantCulture,
                                          DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                          out parsed))
                    return false;
            }
            else
            {
                var combined = $"{date}T{time}Z";
                if (!DateTime.TryParseExact(combined, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture,
                                             DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                             out parsed))
                    return false;
            }

            // Принудительно устанавливаем Kind = Utc
            value = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            return true;
        }

        private static decimal? TryParsePrice(string price)
        {
            if (string.IsNullOrWhiteSpace(price))
                return null;
            var cleaned = price.Replace("$", string.Empty).Replace(",", string.Empty);
            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var val)
                ? val
                : null;
        }
    }
}
