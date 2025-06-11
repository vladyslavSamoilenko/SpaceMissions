using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpaceMissions.Core.Entities;
using SpaceMissions.Infrastructure.Data;
using SpaceMissions.WebAP.DTOs;

namespace SpaceMissions.WebAP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MissionsController : ControllerBase
    {
        private readonly SpaceMissionsDbContext _context;

        public MissionsController(SpaceMissionsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMissions(
            [FromQuery] MissionFilterDto filter,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1 || pageSize < 1)
                return BadRequest("PageNumber and PageSize must be greater than 0.");

            var query = _context.Missions
                .Include(m => m.Rocket)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Company))
                query = query.Where(m => m.Company.Contains(filter.Company));

            if (!string.IsNullOrWhiteSpace(filter.MissionStatus))
                query = query.Where(m => m.MissionStatus == filter.MissionStatus);

            if (filter.StartDate.HasValue)
            {
                var startUtc = DateTime.SpecifyKind(filter.StartDate.Value, DateTimeKind.Utc);
                query = query.Where(m => m.LaunchDateTime >= startUtc);
            }

            if (filter.EndDate.HasValue)
            {
                var endUtc = DateTime.SpecifyKind(filter.EndDate.Value, DateTimeKind.Utc);
                query = query.Where(m => m.LaunchDateTime <= endUtc);
            }

            query = filter.SortBy?.ToLower() switch
            {
                "company" => filter.SortDescending
                    ? query.OrderByDescending(m => m.Company)
                    : query.OrderBy(m => m.Company),

                "missionstatus" => filter.SortDescending
                    ? query.OrderByDescending(m => m.MissionStatus)
                    : query.OrderBy(m => m.MissionStatus),

                "launchdatetime" => filter.SortDescending
                    ? query.OrderByDescending(m => m.LaunchDateTime)
                    : query.OrderBy(m => m.LaunchDateTime),

                _ => query.OrderBy(m => m.Id)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MissionListItemDto
                {
                    Id = m.Id,
                    Company = m.Company,
                    MissionName = m.MissionName,
                    RocketName = m.Rocket!.Name,
                    LaunchDateTime = m.LaunchDateTime,
                    MissionStatus = m.MissionStatus
                })
                .ToListAsync();

            var result = new PaginatedResponseDto<MissionListItemDto>
            {
                PageNumber  = pageNumber,
                PageSize    = pageSize,
                TotalCount  = totalCount,
                TotalPages  = totalPages,
                Items       = items
            };

            return Ok(result);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMissionById(int id)
        {
            var mission = await _context.Missions
                .Include(m => m.Rocket)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mission == null)
                return NotFound();

            var dto = new MissionDto
            {
                Id = mission.Id,
                MissionName = mission.MissionName,
                LaunchDateTime = mission.LaunchDateTime,
                Company = mission.Company,
                Location = mission.Location,
                MissionStatus = mission.MissionStatus ?? string.Empty,
                Price = mission.Price,
                RocketId = mission.RocketId,
                RocketName = mission.Rocket?.Name ?? string.Empty
            };

            return Ok(dto);
        }
        
        [HttpPut("{id}")]
        [Authorize] 
        public async Task<IActionResult> UpdateMission(int id, [FromBody] MissionDto missionDto)
        {
            if (id != missionDto.Id)
            {
                return BadRequest("Id в URL и теле запроса не совпадают.");
            }

            var mission = await _context.Missions
                .AsTracking()             
                .FirstOrDefaultAsync(m => m.Id == id);
            if (mission == null)
            {
                return NotFound();
            }

            // Обновляем поля
            mission.MissionName = missionDto.MissionName;
            mission.LaunchDateTime = missionDto.LaunchDateTime;
            mission.Company = missionDto.Company;
            mission.Location = missionDto.Location;
            mission.MissionStatus = missionDto.MissionStatus;
            mission.Price = missionDto.Price;
            mission.RocketId = missionDto.RocketId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MissionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); 
        }

        // DELETE api/Missions/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRocket(int id)
        {
            var rocket = await _context.Rockets
                .Include(r => r.Missions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rocket == null) return NotFound();

            if (rocket.Missions != null && rocket.Missions.Any())
            {
                return BadRequest("Невозможно удалить ракету, у которой есть связанные миссии.");
            }

            _context.Rockets.Remove(rocket);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateMission([FromBody] MissionDto missionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var mission = new Mission
            {
                MissionName = missionDto.MissionName,
                LaunchDateTime = missionDto.LaunchDateTime,
                Company = missionDto.Company,
                Location = missionDto.Location,
                MissionStatus = missionDto.MissionStatus,
                Price = missionDto.Price,
                RocketId = missionDto.RocketId ?? 0
            };

            _context.Missions.Add(mission);
            await _context.SaveChangesAsync();

            // Получаем имя ракеты для возвращаемого DTO
            var rocket = await _context.Rockets.FindAsync(mission.RocketId);

            var result = new MissionDto
            {
                Id = mission.Id,
                MissionName = mission.MissionName,
                LaunchDateTime = mission.LaunchDateTime,
                Company = mission.Company,
                Location = mission.Location,
                MissionStatus = mission.MissionStatus ?? string.Empty,
                Price = mission.Price,
                RocketId = mission.RocketId,
                RocketName = rocket?.Name ?? string.Empty
            };

            return CreatedAtAction(nameof(GetMissionById), new { id = result.Id }, result);
        }

        // GET: api/Missions/5/rocket
        [HttpGet("{id}/rocket")]
        public async Task<ActionResult<RocketDto>> GetRocketByMissionId(int id)
        {
            var mission = await _context.Missions
                .Include(m => m.Rocket)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mission == null || mission.Rocket == null) return NotFound();

            var rocketDto = new RocketDto
            {
                Id = mission.Rocket.Id,
                Name = mission.Rocket.Name,
                IsActive = mission.Rocket.IsActive
            };

            return Ok(rocketDto);
        }

        private bool MissionExists(int id)
        {
            return _context.Missions.Any(e => e.Id == id);
        }
    }
}
