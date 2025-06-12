﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpaceMissions.Core.Entities;
using SpaceMissions.Infrastructure.Data;
using SpaceMissions.WebAP.DTOs;

namespace SpaceMissions.WebAP.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RocketsController : ControllerBase
{
    private readonly SpaceMissionsDbContext _context;

    public RocketsController(SpaceMissionsDbContext context)
    {
        _context = context;
    }

    // GET: api/Rockets
    [HttpGet(Name = nameof(GetRockets))]                     // ← Name добавлен
    public async Task<ActionResult<Resourсe<IEnumerable<RocketDto>>>> GetRockets()
    {
        var list = await _context.Rockets
            .Select(r => new RocketDto
            {
                Id       = r.Id,
                Name     = r.Name,
                IsActive = r.IsActive
            })
            .ToListAsync();

        // 1) оборачиваем в Resource и добавляем self
        var resource = new Resourсe<IEnumerable<RocketDto>> { Data = list };
        resource.Links.Add(new LinkInfo {
            Href   = Url.Link(nameof(GetRockets), null),
            Rel    = "self",
            Method = "GET"
        });

        return Ok(resource);
    }

    // GET: api/Rockets/5
    [HttpGet("{id}", Name = nameof(GetRocket))]             // ← Name добавлен
    public async Task<ActionResult<Resourсe<RocketDto>>> GetRocket(int id)
    {
        var rocket = await _context.Rockets.FindAsync(id);
        if (rocket == null) return NotFound();

        var dto = new RocketDto
        {
            Id       = rocket.Id,
            Name     = rocket.Name,
            IsActive = rocket.IsActive
        };

        // 2) оборачиваем в Resource + ссылки
        var resource = new Resourсe<RocketDto> { Data = dto };
        resource.Links.AddRange(new[]
        {
            new LinkInfo {
                Href   = Url.Link(nameof(GetRocket), new { id }),
                Rel    = "self",
                Method = "GET"
            },
            new LinkInfo {
                Href   = Url.Link(nameof(UpdateRocket), new { id }),
                Rel    = "update",
                Method = "PUT"
            },
            new LinkInfo {
                Href   = Url.Link(nameof(DeleteRocket), new { id }),
                Rel    = "delete",
                Method = "DELETE"
            },
            new LinkInfo {
                Href   = Url.Link(nameof(GetRockets), null),
                Rel    = "collection",
                Method = "GET"
            },
            new LinkInfo {
                Href   = Url.Link(nameof(GetMissionsByRocketId), new { id }),
                Rel    = "missions",
                Method = "GET"
            }
        });

        return Ok(resource);
    }

    // POST: api/Rockets
    [HttpPost(Name = nameof(PostRocket))]                    // ← Name добавлен
    [Authorize]
    public async Task<IActionResult> PostRocket(RocketDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var rocket = new Rocket
        {
            Name     = dto.Name,
            IsActive = dto.IsActive
        };

        _context.Rockets.Add(rocket);
        await _context.SaveChangesAsync();

        dto.Id = rocket.Id;

        // 3) оборачиваем в Resource + ссылки
        var resource = new Resourсe<RocketDto> { Data = dto };
        resource.Links.AddRange(new[]
        {
            new LinkInfo {
                Href   = Url.Link(nameof(GetRocket), new { id = dto.Id }),
                Rel    = "self",
                Method = "GET"
            },
            new LinkInfo {
                Href   = Url.Link(nameof(GetRockets), null),
                Rel    = "collection",
                Method = "GET"
            }
        });

        return CreatedAtAction(nameof(GetRocket), new { id = dto.Id }, resource);
    }

    // PUT: api/Rockets/5
    [HttpPut("{id}", Name = nameof(UpdateRocket))]           // ← Name добавлен
    [Authorize]
    public async Task<IActionResult> UpdateRocket(int id, RocketDto dto)
    {
        if (id != dto.Id)
            return BadRequest("ID в URL и теле не совпадают.");

        var existing = await _context.Rockets.FindAsync(id);
        if (existing == null)
            return NotFound();

        existing.Name     = dto.Name;
        existing.IsActive = dto.IsActive;

        _context.Entry(existing).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        // 4) в заголовке Link даём возвращаться к самому ресурсу
        Response.Headers.Add("Link",
            $"<{Url.Link(nameof(GetRocket), new { id })}>; rel=\"self\"");

        return NoContent();
    }

    // DELETE: api/Rockets/5
    [HttpDelete("{id}", Name = nameof(DeleteRocket))]       // ← Name добавлен
    [Authorize]
    public async Task<IActionResult> DeleteRocket(int id)
    {
        var rocket = await _context.Rockets.FindAsync(id);
        if (rocket == null) return NotFound();

        _context.Rockets.Remove(rocket);
        await _context.SaveChangesAsync();

        // 5) даём ссылку на коллекцию после удаления
        Response.Headers.Add("Link",
            $"<{Url.Link(nameof(GetRockets), null)}>; rel=\"collection\"");

        return NoContent();
    }

    // GET: api/Rockets/5/missions
    [HttpGet("{id}/missions", Name = nameof(GetMissionsByRocketId))]  // ← Name добавлен
    public async Task<ActionResult<Resourсe<IEnumerable<MissionDto>>>> GetMissionsByRocketId(int id)
    {
        var rocket = await _context.Rockets
            .Include(r => r.Missions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rocket == null) return NotFound();

        var missions = rocket.Missions!
            .Select(m => new MissionDto
            {
                Id              = m.Id,
                MissionName     = m.MissionName,
                LaunchDateTime  = m.LaunchDateTime,
                Company         = m.Company,
                Location        = m.Location,
                MissionStatus   = m.MissionStatus!,
                Price           = m.Price,
                RocketId        = m.RocketId,
                RocketName      = rocket.Name
            })
            .ToList();

        // 6) оборачиваем в Resource + ссылки
        var resource = new Resourсe<IEnumerable<MissionDto>> { Data = missions };
        resource.Links.AddRange(new[]
        {
            new LinkInfo {
                Href   = Url.Link(nameof(GetMissionsByRocketId), new { id }),
                Rel    = "self",
                Method = "GET"
            },
            new LinkInfo {
                Href   = Url.Link(nameof(GetRocket), new { id }),
                Rel    = "rocket",
                Method = "GET"
            },
            new LinkInfo {
                Href   = Url.Link(nameof(GetRockets), null),
                Rel    = "rockets",
                Method = "GET"
            }
        });

        return Ok(resource);
    }

}