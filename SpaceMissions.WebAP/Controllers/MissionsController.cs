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

        /// <summary>
        /// Gets a paginated list of missions with optional filtering and sorting.
        /// </summary>
        [HttpGet(Name = nameof(GetMissions))]
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
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Items = items
            };

            var resource = new Resourсe<PaginatedResponseDto<MissionListItemDto>> { Data = result };
            resource.Links.Add(new LinkInfo
            {
                Href = Url.Link(nameof(GetMissions), new { pageNumber, pageSize }),
                Rel = "self",
                Method = "GET"
            });
            if (result.HasNextPage)
                resource.Links.Add(new LinkInfo
                {
                    Href = Url.Link(nameof(GetMissions), new { pageNumber = pageNumber + 1, pageSize }),
                    Rel = "next",
                    Method = "GET"
                });
            if (result.HasPreviousPage)
                resource.Links.Add(new LinkInfo
                {
                    Href = Url.Link(nameof(GetMissions), new { pageNumber = pageNumber - 1, pageSize }),
                    Rel = "prev",
                    Method = "GET"
                });

            return Ok(resource);
        }

        /// <summary>
        /// Gets a specific mission by id.
        /// </summary>
        [HttpGet("{id}", Name = nameof(GetMissionById))]
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

            var resource = new Resourсe<MissionDto> { Data = dto };
            resource.Links.AddRange(new[]
            {
                new LinkInfo {
                    Href = Url.Link(nameof(GetMissionById), new { id }),
                    Rel = "self",
                    Method = "GET"
                },
                new LinkInfo {
                    Href = Url.Link(nameof(UpdateMission), new { id }),
                    Rel = "update",
                    Method = "PUT"
                },
                new LinkInfo {
                    Href = Url.Link(nameof(DeleteMission), new { id }),
                    Rel = "delete",
                    Method = "DELETE"
                },
                new LinkInfo {
                    Href = Url.Link(nameof(GetRocketByMissionId), new { id }),
                    Rel = "rocket",
                    Method = "GET"
                }
            });

            return Ok(resource);
        }

        /// <summary>
        /// Updates an existing mission.
        /// </summary>
        [HttpPut("{id}", Name = nameof(UpdateMission))]
        [Authorize]
        public async Task<IActionResult> UpdateMission(int id, [FromBody] MissionDto missionDto)
        {
            if (id != missionDto.Id)
            {
                return BadRequest("Id in URL and request body do not match.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var mission = await _context.Missions
                .AsTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (mission == null)
            {
                return NotFound();
            }

            mission.MissionName = missionDto.MissionName;
            mission.LaunchDateTime = missionDto.LaunchDateTime;
            mission.Company = missionDto.Company;
            mission.Location = missionDto.Location;
            mission.MissionStatus = missionDto.MissionStatus;
            mission.Price = missionDto.Price;
            mission.RocketId = missionDto.RocketId ?? 0;

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

            Response.Headers.Add("Link", $"<{Url.Link(nameof(GetMissionById), new { id })}>; rel=\"self\"");
            return NoContent();
        }

        /// <summary>
        /// Deletes a mission by id.
        /// </summary>
        [HttpDelete("{id}", Name = nameof(DeleteMission))]
        [Authorize]
        public async Task<IActionResult> DeleteMission(int id)
        {
            var mission = await _context.Missions
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mission == null)
                return NotFound();

            _context.Missions.Remove(mission);
            await _context.SaveChangesAsync();

            Response.Headers.Add("Link", $"<{Url.Link(nameof(GetMissions), null)}>; rel=\"collection\"");
            return NoContent();
        }

        /// <summary>
        /// Creates a new mission.
        /// </summary>
        [HttpPost(Name = nameof(CreateMission))]
        [Authorize]
        public async Task<IActionResult> CreateMission([FromBody] MissionDto missionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!missionDto.RocketId.HasValue || missionDto.RocketId.Value <= 0)
            {
                return BadRequest("RocketId must be provided and greater than zero.");
            }

            var mission = new Mission
            {
                MissionName = missionDto.MissionName,
                LaunchDateTime = missionDto.LaunchDateTime,
                Company = missionDto.Company,
                Location = missionDto.Location,
                MissionStatus = missionDto.MissionStatus,
                Price = missionDto.Price,
                RocketId = missionDto.RocketId.Value
            };

            _context.Missions.Add(mission);
            await _context.SaveChangesAsync();

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

            var resource = new Resourсe<MissionDto> { Data = result };
            resource.Links.AddRange(new[]
            {
                new LinkInfo {
                    Href = Url.Link(nameof(GetMissionById), new { id = result.Id }),
                    Rel = "self",
                    Method = "GET"
                },
                new LinkInfo {
                    Href = Url.Link(nameof(UpdateMission), new { id = result.Id }),
                    Rel = "update",
                    Method = "PUT"
                },
                new LinkInfo {
                    Href = Url.Link(nameof(DeleteMission), new { id = result.Id }),
                    Rel = "delete",
                    Method = "DELETE"
                },
                new LinkInfo {
                    Href = Url.Link(nameof(GetMissions), null),
                    Rel = "collection",
                    Method = "GET"
                }
            });

            return CreatedAtAction(nameof(GetMissionById), new { id = result.Id }, resource);
        }

        /// <summary>
        /// Gets rocket info for the given mission id.
        /// </summary>
        [HttpGet("{id}/rocket", Name = nameof(GetRocketByMissionId))]
        public async Task<ActionResult<Resourсe<RocketDto>>> GetRocketByMissionId(int id)
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

            var resource = new Resourсe<RocketDto> { Data = rocketDto };
            resource.Links.AddRange(new[]
            {
                new LinkInfo {
                    Href = Url.Link(nameof(GetRocketByMissionId), new { id }),
                    Rel = "self",
                    Method = "GET"
                },
                new LinkInfo {
                    Href = Url.Link(nameof(GetMissionById), new { id }),
                    Rel = "mission",
                    Method = "GET"
                },
                new LinkInfo {
                    Href = Url.Link(nameof(GetMissions), null),
                    Rel = "missions",
                    Method = "GET"
                }
            });

            return Ok(resource);
        }

        private bool MissionExists(int id)
        {
            return _context.Missions.Any(e => e.Id == id);
        }
    }
}
